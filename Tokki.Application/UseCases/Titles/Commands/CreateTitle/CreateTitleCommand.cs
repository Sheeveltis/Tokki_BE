using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Titles.Commands.CreateTitle
{
    public class CreateTitleCommand : IRequest<OperationResult<Title>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TitleRequirementType RequirementType { get; set; }
        public long RequirementQuantity { get; set; }
        public string ColorHex { get; set; } = "#000000";
        public string? IconUrl { get; set; }
    }
}