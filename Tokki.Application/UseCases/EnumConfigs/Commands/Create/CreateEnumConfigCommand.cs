using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EnumConfigs.Commands.Create
{
    public class CreateEnumConfigCommand : IRequest<OperationResult<int>>
    {
        public EnumGroup GroupCode { get; set; }
        public string Key { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
    }
}
