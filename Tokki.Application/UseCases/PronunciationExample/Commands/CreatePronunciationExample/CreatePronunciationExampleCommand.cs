using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationExample.DTOs;
using Tokki.Domain.Enums;

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
        public PronunciationDifficulty Difficulty { get; set; } = PronunciationDifficulty.Medium;
        [JsonIgnore]
        public string? UserId { get; set; }
    }
}
