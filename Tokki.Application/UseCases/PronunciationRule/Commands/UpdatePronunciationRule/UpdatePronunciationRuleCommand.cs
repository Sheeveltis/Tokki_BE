using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.UpdatePronunciationRule
{
    public class UpdatePronunciationRuleCommand : IRequest<OperationResult<bool>>
    {
        public string PronunciationRuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Content { get; set; }
        [JsonIgnore]
        public string? UpdateBy { get; set; }
    }
}
