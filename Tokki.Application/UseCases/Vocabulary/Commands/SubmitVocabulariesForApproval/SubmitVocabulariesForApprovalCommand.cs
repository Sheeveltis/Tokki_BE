using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Vocabulary.Commands.SubmitVocabulariesForApproval
{
    public class SubmitVocabulariesForApprovalCommand
        : IRequest<OperationResult<bool>>
    {
        public List<string> VocabularyIds { get; set; } = new();
    }
}
