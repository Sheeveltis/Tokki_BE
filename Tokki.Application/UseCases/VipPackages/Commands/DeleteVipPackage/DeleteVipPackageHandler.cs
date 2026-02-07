using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.VipPackages.Commands.DeleteVipPackage
{
    public class DeleteVipPackageHandler : IRequestHandler<DeleteVipPackageCommand, OperationResult<bool>>
    {
        private readonly IVipPackageRepository _repository;

        public DeleteVipPackageHandler(IVipPackageRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(DeleteVipPackageCommand request, CancellationToken cancellationToken)
        {
            var package = await _repository.GetByIdAsync(request.Id);

            if (package == null)
                return OperationResult<bool>.Failure(AppErrors.VipPackageNotFound);

            package.IsActive = false; 

            await _repository.UpdateAsync(package);
            return OperationResult<bool>.Success(true);
        }
    }
}