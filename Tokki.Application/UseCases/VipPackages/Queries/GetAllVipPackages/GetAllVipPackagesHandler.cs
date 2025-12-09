using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.VipPackages.Queries.GetAllVipPackages
{
    public class GetAllVipPackagesHandler : IRequestHandler<GetAllVipPackagesQuery, OperationResult<List<VipPackage>>>
    {
        private readonly IVipPackageRepository _repository;

        public GetAllVipPackagesHandler(IVipPackageRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<VipPackage>>> Handle(GetAllVipPackagesQuery request, CancellationToken cancellationToken)
        {
            var packages = await _repository.GetAllAsync(request.IsAdmin);
            return OperationResult<List<VipPackage>>.Success(packages);
        }
    }
}