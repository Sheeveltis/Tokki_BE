using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.VocabularyExample.DTOs;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.AddExamples
{
    public class AddVocabularyExamplesCommand : IRequest<OperationResult<AddVocabularyExamplesResponse>>
    {
        public string VocabularyId { get; set; } = string.Empty;

        public List<VocabularyExampleDto> Examples { get; set; } = new();
    }
}
