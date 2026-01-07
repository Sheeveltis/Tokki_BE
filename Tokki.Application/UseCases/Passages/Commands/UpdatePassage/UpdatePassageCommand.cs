using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.Commands.UpdatePassage
{
    public class UpdatePassageCommand : IRequest<OperationResult<string>>
    {
        public string PassageId { get; set; } = string.Empty;

        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }

        public PassageMediaType? MediaType { get; set; }
    }
}
