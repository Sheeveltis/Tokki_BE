using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.VipPackages.Queries.GetAllVipPackages
{
    public class GetAllVipPackagesQuery : IRequest<OperationResult<List<VipPackage>>>
    {
        public bool IsAdmin { get; set; } = false;
    }
}