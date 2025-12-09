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

            if (!string.IsNullOrEmpty(request.Name) && request.Name != "string")
            {
                package.Name = request.Name;
            }

            if (request.Price > 0)
            {
                package.Price = request.Price;
            }

            if (request.DurationDays > 0)
            {
                package.DurationDays = request.DurationDays;
            }

            if (!string.IsNullOrEmpty(request.Description) && request.Description != "string")
            {
                package.Description = request.Description;
            }

            if (request.IsActive.HasValue)
            {
                package.IsActive = request.IsActive.Value;
            }

            await _repository.UpdateAsync(package);
            return OperationResult<bool>.Success(true);
        }
    }
}