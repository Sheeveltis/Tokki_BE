using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Passages.Commands.DeletePassage
{
    public class DeletePassageCommand : IRequest<OperationResult<bool>>
    {
        public string PassageId { get; set; } = string.Empty;
    }
}
