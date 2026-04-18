using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.UserPronunciation.Commands.CompletePronunciationRule
{
    public class CompletePronunciationRuleCommand : IRequest<OperationResult<bool>>
    {
        public string UserId { get; set; } = null!;
        public string PronunciationRuleId { get; set; } = null!;
    }
}
