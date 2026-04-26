using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.UpdatePronunciationExample
{
    public class UpdatePronunciationExampleCommand : IRequest<OperationResult<Unit>>
    {
        public string ExampleId { get; set; } = string.Empty;
        public string TargetScript { get; set; } = string.Empty;
        public string RawScript { get; set; } = string.Empty;
        public string PhoneticScript { get; set; } = string.Empty;
        public string? Meaning { get; set; }
        public string? AudioUrl { get; set; }
        public PronunciationDifficulty Difficulty { get; set; }
        [JsonIgnore]
        public string? UserId { get; set; }
    }
}
