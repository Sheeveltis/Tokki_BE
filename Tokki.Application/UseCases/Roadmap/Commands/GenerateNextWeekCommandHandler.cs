using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek
{
    public class GenerateNextWeekCommandHandler
        : IRequestHandler<GenerateNextWeekCommand, OperationResult<GenerateNextWeekResult>>
    {
        private readonly IUserRoadmapRepository _repository;
        private readonly IAiRoadmapService _aiRoadmapService;
        private readonly IExamAssemblyService _examAssemblyService;
        private readonly IRoadmapKnowledgeProfileRepository _knowledgeProfileRepository; 
        private readonly IIdGeneratorService _idGeneratorService;

        private const int MaxConsecutiveFail = 2;
        private const double MasteryThreshold = 80.0;

        public GenerateNextWeekCommandHandler(
            IUserRoadmapRepository repository,
            IAiRoadmapService aiRoadmapService,
            IExamAssemblyService examAssemblyService,
            IRoadmapKnowledgeProfileRepository knowledgeProfileRepository, 
            IIdGeneratorService idGeneratorService)
        {
            _repository = repository;
            _aiRoadmapService = aiRoadmapService;
            _examAssemblyService = examAssemblyService;
            _knowledgeProfileRepository = knowledgeProfileRepository;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<GenerateNextWeekResult>> Handle(
            GenerateNextWeekCommand request,
            CancellationToken cancellationToken)
        {
            var currentWeek = await _repository.GetWeekByIdAsync(request.FinishedWeekId, cancellationToken);

            if (currentWeek == null)
                return OperationResult<GenerateNextWeekResult>.Failure("Không tìm thấy tuần tương ứng", 404);

            if (currentWeek.UserRoadmap.UserId != request.UserId)
                return OperationResult<GenerateNextWeekResult>.Failure("Bạn không có quyền thao tác.", 403);

            var roadmap = currentWeek.UserRoadmap;
            int nextWeekIndex = currentWeek.WeekIndex + 1;

            var nextWeek = await _repository.GetWeekByIndexAsync(
                roadmap.UserRoadmapId, nextWeekIndex, cancellationToken);

            currentWeek.Status = RoadmapWeekStatus.Completed;

            if (nextWeek == null)
            {
                await _repository.SaveChangesAsync(cancellationToken);
                return OperationResult<GenerateNextWeekResult>.Failure("Bạn đã hoàn thành!", 200);
            }

            int scorePercent = 0;
            List<string> failedThisWeek = new();   
            List<string> persistentFail = new();  
            List<string> reviewTypes = new();      

            if (!string.IsNullOrEmpty(currentWeek.WeeklyExamId))
            {
                var userExam = await _repository.GetUserExamByExamIdAsync(
                    currentWeek.WeeklyExamId, request.UserId, cancellationToken);

                if (userExam != null)
                {
                    var examQuestions = await _repository.GetExamQuestionsForGradingAsync(
                        currentWeek.WeeklyExamId, cancellationToken);

                    int maxScore = examQuestions.Sum(q => q.Score);
                    scorePercent = maxScore > 0
                        ? (int)Math.Round((double)userExam.Score / maxScore * 100)
                        : 0;

                    var allProfiles = await _knowledgeProfileRepository
                        .GetByRoadmapIdAsync(roadmap.UserRoadmapId, cancellationToken);

                    foreach (var profile in allProfiles.Where(p => p.IsWeakness))
                    {
                        if (profile.ConsecutiveFailWeeks >= MaxConsecutiveFail)
                        {
                            persistentFail.Add(profile.QuestionTypeId);
                        }
                        else if (profile.ConsecutiveFailWeeks == 1
                            && profile.LastEvaluatedWeekIndex == currentWeek.WeekIndex)
                        {
                            reviewTypes.Add(profile.QuestionTypeId);
                        }
                    }
                }
            }

            bool hasWarning = persistentFail.Any();
            string? warningMessage = hasWarning
                ? $"Bạn vẫn chưa nắm vững {persistentFail.Count} dạng câu hỏi sau 2 tuần luyện tập: " +
                  $"{string.Join(", ", persistentFail)}. " +
                  $"Hãy dành thêm thời gian tự ôn luyện các dạng này ngoài lộ trình nhé!"
                : null;

            var aiPlan = await _aiRoadmapService.GenerateNextWeekPlanAsync(
                roadmap.TargetAim,
                nextWeekIndex,
                scorePercent,
                reviewTypes,      
                persistentFail,   
                new List<string>()
            );

            if (aiPlan == null || !aiPlan.Weeks.Any())
                return OperationResult<GenerateNextWeekResult>.Failure("AI không thể tạo tuần tiếp theo.", 500);

            var weekData = aiPlan.Weeks.First();
            nextWeek.WeekFocusGoal = weekData.WeekGoal;
            nextWeek.Status = RoadmapWeekStatus.InProgress;
            nextWeek.DailyTasks.Clear();

            var weeklyScope = weekData.Days
                .SelectMany(d => d.Tasks)
                .Where(t => t.TaskType == "VirtualQuiz" && !string.IsNullOrEmpty(t.QuestionTypeId))
                .Select(t => t.QuestionTypeId!)
                .Distinct()
                .ToList();

            foreach (var dayDto in weekData.Days)
            {
                foreach (var taskDto in dayDto.Tasks)
                {
                    var taskId = _idGeneratorService.GenerateCustom(15);
                    var taskEntity = new RoadmapDailyTask
                    {
                        TaskId = taskId,
                        RoadmapWeekId = nextWeek.RoadmapWeekId,
                        DayIndex = dayDto.DayIndex,
                        Title = taskDto.Title,
                        AiGeneratedContent = taskDto.Content,
                        IsCompleted = false
                    };

                    if (taskDto.TaskType == "LearnTheory")
                    {
                        taskEntity.TaskType = RoadmapTaskType.LearnTheory;
                        if (!string.IsNullOrEmpty(taskDto.GrammarId))
                            taskEntity.GrammarId = taskDto.GrammarId;
                    }
                    else if (taskDto.TaskType == "VirtualQuiz")
                    {
                        taskEntity.TaskType = RoadmapTaskType.VirtualQuiz;
                        if (!string.IsNullOrEmpty(taskDto.QuestionTypeId))
                            taskEntity.QuestionTypeId = taskDto.QuestionTypeId;
                    }
                    else if (taskDto.TaskType == "WeeklyExam")
                    {
                        taskEntity.TaskType = RoadmapTaskType.WeeklyExam;

                        if (weeklyScope.Any())
                        {
                            var examResult = await _examAssemblyService.GenerateWeeklyExamFromScopeAsync(
                                request.UserId,
                                nextWeekIndex,
                                weeklyScope,
                                cancellationToken
                            );

                            if (examResult.IsSuccess)
                            {
                                taskEntity.ExamId = examResult.Data;
                                nextWeek.WeeklyExamId = examResult.Data;
                            }
                        }
                    }

                    nextWeek.DailyTasks.Add(taskEntity);
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);

            var result = new GenerateNextWeekResult
            {
                IsGenerated = true,
                HasWarning = hasWarning,
                WarningMessage = warningMessage,
                PersistentWeakTypeIds = persistentFail
            };

            return OperationResult<GenerateNextWeekResult>.Success(result);
        }
    }
}