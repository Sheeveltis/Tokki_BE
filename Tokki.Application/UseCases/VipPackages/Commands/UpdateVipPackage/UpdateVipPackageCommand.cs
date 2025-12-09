using MediatR;
using System.Text.Json.Serialization; 
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.VipPackages.Commands.UpdateVipPackage
{
    public class UpdateVipPackageCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore]
        public string? Id { get; set; } 

        public string Name { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string Description { get; set; }
        public bool? IsActive { get; set; } 
    }
}