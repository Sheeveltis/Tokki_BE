using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek
{
    public class GenerateNextWeekCommandHandler : IRequestHandler<GenerateNextWeekCommand, OperationResult<bool>>
    {
        private readonly IUserRoadmapRepository _repository;
        private readonly IAiRoadmapService _aiRoadmapService;
        private readonly IExamAssemblyService _examAssemblyService;
        private readonly IIdGeneratorService _idGeneratorService;

        public GenerateNextWeekCommandHandler(
            IUserRoadmapRepository repository,
            IAiRoadmapService aiRoadmapService,
            IExamAssemblyService examAssemblyService,
            IIdGeneratorService idGeneratorService)
        {
            _repository = repository;
            _aiRoadmapService = aiRoadmapService;
            _examAssemblyService = examAssemblyService; 
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<bool>> Handle(GenerateNextWeekCommand request, CancellationToken cancellationToken)
        {
            var currentWeek = await _repository.GetWeekByIdAsync(request.FinishedWeekId, cancellationToken);

            if (currentWeek == null)
                return OperationResult<bool>.Failure("Không tìm thấy tuần tương ứng", 404);

            if (currentWeek.UserRoadmap.UserId != request.UserId)
                return OperationResult<bool>.Failure("Bạn không có quyền thao tác.", 403);

            var roadmap = currentWeek.UserRoadmap;
            int nextWeekIndex = currentWeek.WeekIndex + 1;

            var nextWeek = await _repository.GetWeekByIndexAsync(roadmap.UserRoadmapId, nextWeekIndex, cancellationToken);

            if (nextWeek == null)
            {
                currentWeek.Status = RoadmapWeekStatus.Completed;
                await _repository.SaveChangesAsync(cancellationToken);
                return OperationResult<bool>.Failure("Bạn đã hoàn thành!", 200);
            }

            currentWeek.Status = RoadmapWeekStatus.Completed;

            int score = 0;
            List<string> detectedWeaknesses = new();

            if (!string.IsNullOrEmpty(currentWeek.WeeklyExamId))
            {
                var userExam = await _repository.GetUserExamByExamIdAsync(
                    currentWeek.WeeklyExamId, request.UserId, cancellationToken);

                if (userExam != null)
                {
                    var examQuestions = await _repository.GetExamQuestionsForGradingAsync(
                        currentWeek.WeeklyExamId, cancellationToken);

                    int maxScore = examQuestions.Sum(q => q.Score);
                    score = maxScore > 0
                        ? (int)Math.Round((double)userExam.Score / maxScore * 100)
                        : 0;

                    if (score < 80)
                    {
                        detectedWeaknesses = await _repository.GetWeakQuestionTypesFromExamAsync(
                            userExam.UserExamId, cancellationToken);
                    }
                }
            }
            var aiPlan = await _aiRoadmapService.GenerateNextWeekPlanAsync(
                roadmap.TargetAim,
                nextWeekIndex,
                score,
                detectedWeaknesses,
                new List<string>()
            );

            if (aiPlan == null || !aiPlan.Weeks.Any())
                return OperationResult<bool>.Failure("AI không thể tạo tuần tiếp theo.", 500);

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
            return OperationResult<bool>.Success(true);
        }
    }
}