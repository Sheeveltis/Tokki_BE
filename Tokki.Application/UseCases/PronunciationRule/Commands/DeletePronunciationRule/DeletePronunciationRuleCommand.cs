using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.DeletePronunciationRule
{
    public class DeletePronunciationRuleCommand : IRequest<OperationResult<bool>>
    {
        public string PronunciationRuleId { get; set; } = string.Empty;

        public DeletePronunciationRuleCommand(string id)
        {
            PronunciationRuleId = id;
        }
    }
}
