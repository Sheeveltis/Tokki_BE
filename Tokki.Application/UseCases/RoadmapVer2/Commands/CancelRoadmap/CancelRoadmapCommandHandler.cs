using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.RoadmapVer2.Commands.CancelRoadmap
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
            // 1. Tìm Roadmap đang hoạt động (Hoặc theo ID nếu được cung cấp)
            var roadmap = await _repository.GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (roadmap == null || (!string.IsNullOrEmpty(request.RoadmapId) && roadmap.UserRoadmapId != request.RoadmapId))
                return OperationResult<bool>.Failure("Không tìm thấy lộ trình đang hoạt động để hủy.", 404);

            // 2. Chuyển trạng thái Roadmap sang Dropped
            roadmap.CurrentStatus = UserRoadmapStatus.Dropped;

            // 3. Chuyển tất cả điểm yếu thuộc về lộ trình này sang trạng thái "Bỏ" (Status = 3)
            var weaknesses = await _weaknessRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            var roadmapWeaknesses = weaknesses.Where(w => w.RoadmapId == roadmap.UserRoadmapId).ToList();

            foreach (var weakness in roadmapWeaknesses)
            {
                weakness.Status = 3; // 3: Dropped/Cancelled
                weakness.UpdatedAt = DateTime.UtcNow;
            }

            // 4. Lưu thay đổi
            await _repository.SaveChangesAsync(cancellationToken);
            await _weaknessRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, "Đã hủy lộ trình và các mục tiêu liên quan thành công.");
        }
    }
}
