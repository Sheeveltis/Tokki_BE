using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.VipPackages.Commands.DeleteVipPackage
{
    public class DeleteVipPackageCommand : IRequest<OperationResult<bool>>
    {
        public string Id { get; set; }
    }
}