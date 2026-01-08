using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Vocabulary.Commands.RejectVocabulary
{
    public class RejectVocabulariesCommand : IRequest<OperationResult<bool>>
    {
        public List<string> VocabularyIds { get; set; } = new();
        public string Reason { get; set; } = string.Empty; 
    }
}
