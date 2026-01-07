using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Vocabulary.Commands.ApproveVocabulary
{
    public class ApproveVocabularyCommand
        : IRequest<OperationResult<bool>>
    {
        public string VocabularyId { get; set; } = string.Empty;
    }
}
