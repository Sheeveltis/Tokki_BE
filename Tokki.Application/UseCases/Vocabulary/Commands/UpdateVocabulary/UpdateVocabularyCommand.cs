using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary
{
    public class UpdateVocabularyCommand : IRequest<OperationResult<VocabularyResponseDto>>
    {
        public string VocabularyId { get; set; } = string.Empty;
        public VocabularyUpdateDto UpdateData { get; set; } = new();
    }
}
