using MediatR;
using System.Linq;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Queries
{
    public class GetWeekProgressQueryHandler : IRequestHandler<GetWeekProgressQuery, OperationResult<int>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;

        public GetWeekProgressQueryHandler(IUserRoadmapRepository userRoadmapRepository)
        {
            _userRoadmapRepository = userRoadmapRepository;
        }

        public async Task<OperationResult<int>> Handle(GetWeekProgressQuery request, CancellationToken cancellationToken)
        {
            var week = await _userRoadmapRepository.GetWeekByIdAsync(request.RoadmapWeekId, cancellationToken);

            if (week == null)
                return OperationResult<int>.Failure("Không tìm thấy tuần học.", 404);

            int progressPercent = week.DailyTasks == null || week.DailyTasks.Count == 0
                ? 0
                : (int)((double)week.DailyTasks.Count(t => t.IsCompleted) / week.DailyTasks.Count * 100);

            return OperationResult<int>.Success(progressPercent);
        }
    }
}