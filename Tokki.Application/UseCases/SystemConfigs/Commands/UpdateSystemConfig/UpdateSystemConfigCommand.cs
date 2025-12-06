using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.SystemConfigs.Commands.Update
{
    public class UpdateSystemConfigCommand : IRequest<OperationResult<string>>
    {
        public string Key { get; set; } = string.Empty; 
        public string? Value { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}