using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.MiniGame.Commands.AssignWordleVocab
{
    public class AssignWordleVocabCommand : IRequest<OperationResult<bool>>
    {
        public string DailyWordleId { get; set; } = string.Empty;
        public string VocabularyId { get; set; } = string.Empty;
    }
}
