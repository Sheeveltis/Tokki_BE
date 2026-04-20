using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.ChangeSortOrder
{
    public class ChangePronunciationExampleSortOrderCommand : IRequest<OperationResult<Unit>>
    {
        public string ExampleId { get; set; } = string.Empty;
        public int NewSortOrder { get; set; }
    }
}
