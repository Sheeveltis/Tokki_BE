using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.Commands.CreatePassage
{
    public class CreatePassageCommand : IRequest<OperationResult<string>>
    {
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public PassageMediaType MediaType { get; set; } = PassageMediaType.Text;
    }
}
