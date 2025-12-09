using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.VipPackages.Commands.UpdateVipPackage
{
    public class UpdateVipPackageHandler : IRequestHandler<UpdateVipPackageCommand, OperationResult<bool>>
    {
        private readonly IVipPackageRepository _repository;

        public UpdateVipPackageHandler(IVipPackageRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateVipPackageCommand request, CancellationToken cancellationToken)
        {
            var package = await _repository.GetByIdAsync(request.Id);
            if (package == null) return OperationResult<bool>.Failure("Gói VIP không tồn tại.");

            package.Name = request.Name;
            package.Price = request.Price;
            package.DurationDays = request.DurationDays;
            package.Description = request.Description;
            package.IsActive = request.IsActive;

            await _repository.UpdateAsync(package);
            return OperationResult<bool>.Success(true);
        }
    }
}