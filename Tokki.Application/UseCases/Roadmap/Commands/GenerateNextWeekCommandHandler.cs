using MediatR;
using Microsoft.EntityFrameworkCore;
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
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IExamRepository _examRepository;

        public GenerateNextWeekCommandHandler(
            IUserRoadmapRepository repository,
            IAiRoadmapService aiRoadmapService,
            IIdGeneratorService idGeneratorService)
        {
            _repository = repository;
            _aiRoadmapService = aiRoadmapService;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<bool>> Handle(GenerateNextWeekCommand request, CancellationToken cancellationToken)
        {
            var currentWeek = await _repository.GetWeekByIdAsync(request.FinishedWeekId, cancellationToken);

            if (currentWeek == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy tuần tương ứng", 404);
            }

            var roadmap = currentWeek.UserRoadmap;
            int nextWeekIndex = currentWeek.WeekIndex + 1;

            var nextWeek = await _repository.GetWeekByIndexAsync(roadmap.UserRoadmapId, nextWeekIndex, cancellationToken);

            if (nextWeek == null)
            {
                return OperationResult<bool>.Failure("Bạn đã hoàn thành!", 200);
            }

            int score = 0;
            List<string> detectedWeaknesses = new List<string>();

            var userExam = await _repository.GetUserExamByExamIdAsync(currentWeek.WeeklyExamId, request.UserId, cancellationToken);

            if (userExam != null)
            {
                score = userExam.Score;

                if (score < 70)
                {
                    detectedWeaknesses = await _repository.GetWeakQuestionTypesFromExamAsync(userExam.UserExamId, cancellationToken);
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
            {
                return OperationResult<bool>.Failure("AI không thể tạo tuần tiếp theo.", 500);
            }

            var weekData = aiPlan.Weeks.First();
            nextWeek.WeekFocusGoal = weekData.WeekGoal;
            nextWeek.Status = RoadmapWeekStatus.InProgress; 

            nextWeek.DailyTasks.Clear();

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
                        TaskType = taskDto.TaskType == "WeeklyExam" ? RoadmapTaskType.WeeklyExam :
                                   (taskDto.TaskType == "VirtualQuiz" ? RoadmapTaskType.VirtualQuiz : RoadmapTaskType.LearnTheory),
                        IsCompleted = false
                    };

                    if (!string.IsNullOrEmpty(taskDto.GrammarId)) taskEntity.GrammarId = taskDto.GrammarId;
                    if (!string.IsNullOrEmpty(taskDto.QuestionTypeId)) taskEntity.QuestionTypeId = taskDto.QuestionTypeId;

                    nextWeek.DailyTasks.Add(taskEntity);
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}