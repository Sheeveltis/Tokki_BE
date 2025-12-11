using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.VipPackages.Commands.CreateVipPackage
{
    public class CreateVipPackageCommand : IRequest<OperationResult<string>>
    {
        public string Name { get; set; }
        public string? PackageType { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string? Description { get; set; }
    }
}