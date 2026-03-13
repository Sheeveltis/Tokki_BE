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

        public CancelRoadmapCommandHandler(IUserRoadmapRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(
            CancelRoadmapCommand request,
            CancellationToken cancellationToken)
        {
            var roadmap = await _repository
                .GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (roadmap == null)
                return OperationResult<bool>.Failure(
                    "Không có lộ trình nào đang hoạt động để hủy.", 404);

            roadmap.CurrentStatus = UserRoadmapStatus.Dropped;

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, "Đã hủy lộ trình thành công.");
        }
    }
}