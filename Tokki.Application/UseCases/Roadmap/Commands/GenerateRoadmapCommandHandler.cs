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
                    request.DurationDays,
                    request.Weaknesses
                );

                if (aiPlan == null || aiPlan.Weeks == null || !aiPlan.Weeks.Any())
                {
                    return OperationResult<string>.Failure("AI không thể tạo lộ trình lúc này. Vui lòng thử lại.", 503);
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
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var weekDto in aiPlan.Weeks)
                {
                    var weekId = _idGeneratorService.GenerateCustom(15);
                    var weekEntity = new RoadmapWeek
                    {
                        RoadmapWeekId = weekId,
                        UserRoadmapId = roadmapId,
                        WeekIndex = weekDto.WeekIndex,
                        WeekFocusGoal = weekDto.WeekGoal,
                        Status = weekDto.WeekIndex == 1 ? RoadmapWeekStatus.InProgress : RoadmapWeekStatus.Locked,
                        FromDate = roadmap.StartDate.AddDays((weekDto.WeekIndex - 1) * 7),
                        ToDate = roadmap.StartDate.AddDays(weekDto.WeekIndex * 7),
                        DailyTasks = new List<RoadmapDailyTask>() 
                    };
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
                    roadmap.Weeks.Add(weekEntity);
                }

                await _userRoadmapRepository.AddAsync(roadmap);
                await _userRoadmapRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(roadmapId, 201, "Tạo lộ trình học tập thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi tạo Roadmap");
                if (ex.InnerException != null) _logger.LogError($"Inner: {ex.InnerException.Message}");

                return OperationResult<string>.Failure("Lỗi hệ thống. Vui lòng thử lại sau.", 500);
            }
        }
    }
}