using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs;
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
        private readonly IUserWeaknessRepository _userWeaknessRepository;
        private readonly IUserExamRepository _userExamRepository;

        public GenerateRoadmapCommandHandler(
            IAiRoadmapService aiRoadmapService,
            IExamAssemblyService examAssemblyService,
            IIdGeneratorService idGeneratorService,
            IUserRoadmapRepository userRoadmapRepository,
            IUserWeaknessRepository userWeaknessRepository,
            IUserExamRepository userExamRepository, 
            ILogger<GenerateRoadmapCommandHandler> logger)
        {
            _aiRoadmapService = aiRoadmapService;
            _examAssemblyService = examAssemblyService;
            _idGeneratorService = idGeneratorService;
            _userRoadmapRepository = userRoadmapRepository;
            _userWeaknessRepository = userWeaknessRepository;
            _userExamRepository = userExamRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(GenerateRoadmapCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Bắt đầu tạo lộ trình {request.DurationDays} ngày cho User: {request.UserId}");

            try
            {
                var activeRoadmap = await _userRoadmapRepository
                    .GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

                if (activeRoadmap != null)
                    return OperationResult<string>.Failure(
                        "Bạn đang có một lộ trình học đang hoạt động. Vui lòng hoàn thành hoặc hủy lộ trình cũ trước khi tạo mới.", 400);

                var weaknesses = new List<string>();

                if (!string.IsNullOrEmpty(request.UserExamId))
                {
                    var questionTypes = await _userExamRepository
                        .GetIncorrectQuestionTypesByExamIdAsync(
                            request.UserExamId, cancellationToken);

                    weaknesses = questionTypes
                        .Select(qt => qt.QuestionTypeId)
                        .Distinct()
                        .ToList();
                }

                if (weaknesses.Any())
                {
                    var validIds = await _userRoadmapRepository
                        .GetValidQuestionTypeIdsAsync(weaknesses, cancellationToken);

                    var invalidIds = weaknesses.Except(validIds).ToList();
                    if (invalidIds.Any())
                        _logger.LogWarning($"Loại bỏ {invalidIds.Count} questionTypeId không hợp lệ: {string.Join(", ", invalidIds)}");

                    weaknesses = validIds;
                }

                var weakTypeInfos = weaknesses.Any()
                    ? await _userRoadmapRepository.GetQuestionTypeMenuAsync(
                        weaknesses, cancellationToken)
                    : new List<QuestionTypeMenuItem>();

                var grammarMenu = await _userRoadmapRepository.GetGrammarMenuAsync(
                    weaknesses, request.CurrentLevel, cancellationToken);

                var allLevelTypeIds = await _userRoadmapRepository
                    .GetValidQuestionTypeIdsByLevelAsync(request.CurrentLevel, cancellationToken);

                var questionTypeMenu = await _userRoadmapRepository
                    .GetQuestionTypeMenuAsync(allLevelTypeIds, cancellationToken);

                var aiPlan = await _aiRoadmapService.GenerateStudyPlanAsync(
                    request.TargetAim,
                    request.CurrentLevel,
                    7,
                    weaknesses,
                    weakTypeInfos,
                    grammarMenu,
                    questionTypeMenu
                );

                if (aiPlan == null || aiPlan.Weeks == null || !aiPlan.Weeks.Any())
                    return OperationResult<string>.Failure("AI không thể tạo lộ trình. Vui lòng thử lại.", 503);

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

                            var weeklyScope = weekDto.Days
                                .SelectMany(d => d.Tasks)
                                .Where(t => t.TaskType == "VirtualQuiz"
                                         && !string.IsNullOrEmpty(t.QuestionTypeId))
                                .Select(t => t.QuestionTypeId)
                                .Distinct()
                                .ToList();

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

                                        if (weeklyScope.Any())
                                        {
                                            var examResult = await _examAssemblyService
                                                .GenerateWeeklyExamFromScopeAsync(
                                                    request.UserId,
                                                    i,
                                                    weeklyScope,
                                                    cancellationToken);

                                            if (examResult.IsSuccess)
                                            {
                                                taskEntity.ExamId = examResult.Data;
                                                weekEntity.WeeklyExamId = examResult.Data;
                                            }
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

                if (weaknesses.Any())
                {
                    foreach (var weakTypeId in weaknesses)
                    {
                        await _userWeaknessRepository.AddAsync(new UserWeakness
                        {
                            Id = _idGeneratorService.GenerateCustom(15),
                            UserId = request.UserId,
                            RoadmapId = roadmapId,
                            QuestionTypeId = weakTypeId,
                            Status = 0,
                            InitialScore = 0,
                            CurrentScore = 0,
                            CreatedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }
                    await _userWeaknessRepository.SaveChangesAsync(cancellationToken);
                }

                return OperationResult<string>.Success(
                    roadmapId, 201, "Tạo lộ trình thành công (Week 1 sẵn sàng)!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo Roadmap");
                return OperationResult<string>.Failure("Lỗi hệ thống.", 500);
            }
        }
    }
}