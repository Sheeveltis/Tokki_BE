using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary
{
    public class DeleteVocabularyCommand : IRequest<OperationResult<bool>>
    {
        public string VocabularyId { get; set; } = string.Empty;
    }
}
