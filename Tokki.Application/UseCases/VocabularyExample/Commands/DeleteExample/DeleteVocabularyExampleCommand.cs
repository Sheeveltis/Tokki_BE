using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.DeleteExample
{
    public class DeleteVocabularyExampleCommand : IRequest<OperationResult<bool>>
    {
        public string ExampleId { get; set; } = string.Empty;
    }
}
