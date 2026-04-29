using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.EnumConfigs.Commands.Update
{
    public class UpdateEnumConfigCommand : IRequest<OperationResult<bool>>
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
