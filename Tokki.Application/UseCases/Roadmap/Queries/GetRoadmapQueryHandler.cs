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

            var totalTasks = roadmapEntity.Weeks.SelectMany(w => w.DailyTasks).Count();
            var completedTasks = roadmapEntity.Weeks.SelectMany(w => w.DailyTasks).Count(t => t.IsCompleted);
            var percent = totalTasks == 0 ? 0 : (int)((double)completedTasks / totalTasks * 100);

            var result = new RoadmapViewModel
            {
                UserRoadmapId = roadmapEntity.UserRoadmapId,
                TargetAim = roadmapEntity.TargetAim,
                Assessment = roadmapEntity.OverallAiAssessment,
                ProgressPercent = percent,
                Weeks = roadmapEntity.Weeks.OrderBy(w => w.WeekIndex).Select(w => new WeekViewModel
                {
                    RoadmapWeekId = w.RoadmapWeekId,
                    WeekIndex = w.WeekIndex,
                    FocusGoal = w.WeekFocusGoal,
                    Status = w.Status.ToString(),
                    Tasks = w.DailyTasks.OrderBy(t => t.DayIndex).Select(t => new TaskViewModel
                    {
                        TaskId = t.TaskId,
                        Title = t.Title,
                        TaskType = t.TaskType.ToString(),
                        IsCompleted = t.IsCompleted,
                        DayIndex = t.DayIndex,
                        Content = t.AiGeneratedContent
                    }).ToList()
                }).ToList()
            };

            return OperationResult<RoadmapViewModel>.Success(result);
        }
    }
}