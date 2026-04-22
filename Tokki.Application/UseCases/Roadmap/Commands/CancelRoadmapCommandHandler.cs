using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.CancelRoadmap
{
    public class CancelRoadmapCommandHandler
        : IRequestHandler<CancelRoadmapCommand, OperationResult<bool>>
    {
        private readonly IUserRoadmapRepository _repository;
        private readonly IUserWeaknessRepository _weaknessRepository;

        public CancelRoadmapCommandHandler(
            IUserRoadmapRepository repository,
            IUserWeaknessRepository weaknessRepository)
        {
            _repository = repository;
            _weaknessRepository = weaknessRepository;
        }

        public async Task<OperationResult<bool>> Handle(
            CancelRoadmapCommand request,
            CancellationToken cancellationToken)
        {
            var roadmap = await _repository
                .GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (roadmap == null
                || (!string.IsNullOrEmpty(request.RoadmapId)
                    && roadmap.UserRoadmapId != request.RoadmapId))
                return OperationResult<bool>.Failure(
                    "Không có lộ trình nào đang hoạt động để hủy.", 404);

            roadmap.CurrentStatus = UserRoadmapStatus.Dropped;

            var weaknesses = await _weaknessRepository
                .GetByUserIdAsync(request.UserId, cancellationToken);

            var roadmapWeaknesses = weaknesses
                .Where(w => w.RoadmapId == roadmap.UserRoadmapId)
                .ToList();

            foreach (var weakness in roadmapWeaknesses)
            {
                weakness.Status = 3; 
                weakness.UpdatedAt = DateTime.UtcNow;
            }

            await _repository.SaveChangesAsync(cancellationToken);
            await _weaknessRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, "Đã hủy lộ trình thành công.");
        }
    }
}