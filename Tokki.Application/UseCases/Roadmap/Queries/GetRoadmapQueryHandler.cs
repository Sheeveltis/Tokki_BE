using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap
{
    public class GetRoadmapQueryHandler : IRequestHandler<GetRoadmapQuery, OperationResult<RoadmapViewModel>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;

        public GetRoadmapQueryHandler(IUserRoadmapRepository userRoadmapRepository)
        {
            _userRoadmapRepository = userRoadmapRepository;
        }

        public async Task<OperationResult<RoadmapViewModel>> Handle(GetRoadmapQuery request, CancellationToken cancellationToken)
        {
            var roadmapEntity = await _userRoadmapRepository.GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (roadmapEntity == null)
            {
                return OperationResult<RoadmapViewModel>.Failure("Người dùng chưa có lộ trình nào đang kích hoạt.", 404);
            }

            var result = new RoadmapViewModel
            {
                UserRoadmapId = roadmapEntity.UserRoadmapId,
                TargetAim = roadmapEntity.TargetAim,
                Assessment = roadmapEntity.OverallAiAssessment,
                Weeks = roadmapEntity.Weeks.OrderBy(w => w.WeekIndex).Select(w => new WeekViewModel
                {
                    RoadmapWeekId = w.RoadmapWeekId,
                    WeekIndex = w.WeekIndex,
                    FocusGoal = w.WeekFocusGoal,
                    Status = w.Status.ToString(),
                    ProgressPercent = w.DailyTasks.Count == 0 ? 0
                    : (int)((double)w.DailyTasks.Count(t => t.IsCompleted) / w.DailyTasks.Count * 100),
                    Tasks = w.DailyTasks.OrderBy(t => t.DayIndex).Select(t => new TaskViewModel
                    {
                        TaskId = t.TaskId,
                        Title = t.Title,
                        TaskType = t.TaskType.ToString(),
                        Skill = t.QuestionType != null ? t.QuestionType.Skill.ToString() : null,
                        IsCompleted = t.IsCompleted,
                        DayIndex = t.DayIndex,
                        HasContent = !string.IsNullOrEmpty(t.AiGeneratedContent),
                        ExamId = t.ExamId,
                        QuestionTypeId = t.QuestionTypeId ?? t.TargetQuestionTypeId
                    }).ToList()
                }).ToList()
            };

            return OperationResult<RoadmapViewModel>.Success(result);
        }
    }
}