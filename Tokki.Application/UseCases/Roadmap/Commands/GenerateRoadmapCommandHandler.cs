using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapCommandHandler : IRequestHandler<GenerateRoadmapCommand, OperationResult<string>>
    {
        private readonly IAiRoadmapService _aiRoadmapService;
        private readonly IExamAssemblyService _examAssemblyService;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<GenerateRoadmapCommandHandler> _logger;
        private readonly IUserRoadmapRepository _userRoadmapRepository;

        public GenerateRoadmapCommandHandler(
            IAiRoadmapService aiRoadmapService,
            IExamAssemblyService examAssemblyService,
            IIdGeneratorService idGeneratorService,
            IUserRoadmapRepository userRoadmapRepository,
            ILogger<GenerateRoadmapCommandHandler> logger)
        {
            _aiRoadmapService = aiRoadmapService;
            _examAssemblyService = examAssemblyService;
            _idGeneratorService = idGeneratorService;
            _userRoadmapRepository = userRoadmapRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(GenerateRoadmapCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Bắt đầu tạo lộ trình {request.DurationDays} ngày cho User: {request.UserId}");

            try
            {
                var aiPlan = await _aiRoadmapService.GenerateStudyPlanAsync(
                    request.TargetAim,
                    request.CurrentLevel,
                    7,
                    request.Weaknesses
                );

                if (aiPlan == null || aiPlan.Weeks == null || !aiPlan.Weeks.Any())
                {
                    return OperationResult<string>.Failure("AI không thể tạo lộ trình. Vui lòng thử lại.", 503);
                }

                var roadmapId = _idGeneratorService.GenerateCustom(15);
                var roadmap = new UserRoadmap
                {
                    UserRoadmapId = roadmapId,
                    UserId = request.UserId,
                    TargetAim = request.TargetAim,
                    CurrentLevel = request.CurrentLevel,
                    DurationDays = request.DurationDays,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(request.DurationDays),
                    CurrentStatus = UserRoadmapStatus.Active,
                    OverallAiAssessment = aiPlan.Assessment,
                    CreatedAt = DateTime.UtcNow,
                    Weeks = new List<RoadmapWeek>()
                };

                int totalWeeks = (int)Math.Ceiling((double)request.DurationDays / 7);

                for (int i = 1; i <= totalWeeks; i++)
                {
                    var weekId = _idGeneratorService.GenerateCustom(15);
                    var weekEntity = new RoadmapWeek
                    {
                        RoadmapWeekId = weekId,
                        UserRoadmapId = roadmapId,
                        WeekIndex = i,
                        FromDate = roadmap.StartDate.AddDays((i - 1) * 7),
                        ToDate = roadmap.StartDate.AddDays(i * 7),
                        DailyTasks = new List<RoadmapDailyTask>()
                    };

                    if (i == 1)
                    {
                        weekEntity.Status = RoadmapWeekStatus.InProgress;
                        var weekDto = aiPlan.Weeks.FirstOrDefault();

                        if (weekDto != null)
                        {
                            weekEntity.WeekFocusGoal = weekDto.WeekGoal;

                            foreach (var dayDto in weekDto.Days)
                            {
                                foreach (var taskDto in dayDto.Tasks)
                                {
                                    var taskId = _idGeneratorService.GenerateCustom(15);
                                    var taskEntity = new RoadmapDailyTask
                                    {
                                        TaskId = taskId,
                                        RoadmapWeekId = weekId,
                                        DayIndex = dayDto.DayIndex,
                                        Title = taskDto.Title,
                                        AiGeneratedContent = taskDto.Content,
                                        IsCompleted = false
                                    };

                                    if (taskDto.TaskType == "LearnTheory")
                                    {
                                        taskEntity.TaskType = RoadmapTaskType.LearnTheory;
                                        taskEntity.GrammarId = taskDto.GrammarId;
                                    }
                                    else if (taskDto.TaskType == "VirtualQuiz")
                                    {
                                        taskEntity.TaskType = RoadmapTaskType.VirtualQuiz;
                                        taskEntity.QuestionTypeId = taskDto.QuestionTypeId;
                                    }
                                    else if (taskDto.TaskType == "WeeklyExam")
                                    {
                                        taskEntity.TaskType = RoadmapTaskType.WeeklyExam;
                                        var examTemplateId = "TEMPLATE_DEFAULT_ID";
                                        DifficultyLevel targetLevel = DifficultyLevel.Easy;
                                        if (request.TargetAim == TargetAimLevel.Topik_II)
                                        {
                                            targetLevel = DifficultyLevel.Medium;
                                        }
                                        var examResult = await _examAssemblyService.GenerateWeeklyExamAsync(
                                            examTemplateId,
                                            request.UserId,
                                            weekDto.WeekIndex,
                                            request.Weaknesses,
                                            targetLevel,
                                            cancellationToken
                                        );

                                        if (examResult.IsSuccess)
                                        {
                                            taskEntity.ExamId = examResult.Data;
                                        }
                                    }
                                    weekEntity.DailyTasks.Add(taskEntity);
                                }
                            }
                        }
                    }
                    else 
                    {
                        weekEntity.Status = RoadmapWeekStatus.Locked;
                        weekEntity.WeekFocusGoal = "Nội dung sẽ được cập nhật dựa trên kết quả tuần trước.";
                    }

                    roadmap.Weeks.Add(weekEntity);
                }

                await _userRoadmapRepository.AddAsync(roadmap);
                await _userRoadmapRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(roadmapId, 201, "Tạo lộ trình thành công (Week 1 sẵn sàng)!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo Roadmap");
                return OperationResult<string>.Failure("Lỗi hệ thống.", 500);
            }
        }
    }
}