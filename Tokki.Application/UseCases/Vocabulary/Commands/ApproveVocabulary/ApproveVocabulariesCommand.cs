using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Vocabulary.Commands.ApproveVocabulary
{
    public class ApproveVocabulariesCommand
         : IRequest<OperationResult<bool>>
    {
        public List<string> VocabularyIds { get; set; } = new();
    }
}
