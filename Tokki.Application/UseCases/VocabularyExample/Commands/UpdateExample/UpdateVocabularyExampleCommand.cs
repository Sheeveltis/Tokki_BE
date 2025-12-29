using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.VocabularyExample.DTOs;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.UpdateExample
{
    public class UpdateVocabularyExampleCommand
        : IRequest<OperationResult<VocabularyExampleResponse>>
    {
        public string ExampleId { get; set; } = string.Empty;
        public VocabularyExampleUpdateDto UpdateData { get; set; } = new();
    }
}
