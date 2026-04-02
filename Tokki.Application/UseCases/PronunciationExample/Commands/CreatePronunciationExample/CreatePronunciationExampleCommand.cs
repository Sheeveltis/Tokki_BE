using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationExample.DTOs;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.CreatePronunciationExample
{
    public class CreatePronunciationExampleCommand : IRequest<OperationResult<string>>
    {
        public string PronunciationRuleId { get; set; } = string.Empty;
        public string TargetScript { get; set; } = string.Empty;
        public string RawScript { get; set; } = string.Empty;
        public string PhoneticScript { get; set; } = string.Empty;
        public string? Meaning { get; set; }
        public string? AudioUrl { get; set; }
        public int SortOrder { get; set; }
        public string? UserId { get; set; }
    }
}
