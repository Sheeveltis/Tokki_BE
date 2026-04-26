using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.Reorder
{
    public class ChangePronunciationRuleSortOrderCommand : IRequest<OperationResult<Unit>>
    {
        public string PronunciationRuleId { get; set; } = string.Empty;
        public int NewSortOrder { get; set; }
    }
}
