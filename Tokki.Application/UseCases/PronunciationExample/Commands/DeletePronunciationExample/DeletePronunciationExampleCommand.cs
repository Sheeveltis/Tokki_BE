using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.DeletePronunciationExample
{
    public class DeletePronunciationExampleCommand : IRequest<OperationResult<Unit>>
    {
        public string ExampleId { get; set; } = string.Empty;
    }
}
