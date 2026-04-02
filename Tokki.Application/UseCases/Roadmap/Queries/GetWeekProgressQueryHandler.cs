using MediatR;
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

            return OperationResult<int>.Success(week.ProgressPercent);
        }
    }
}
