using MediatR;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapCommand : IRequest<OperationResult<string>>
    {
        public string TargetAim { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public List<string> Weaknesses { get; set; } = new List<string>();
        public string CurrentLevel { get; set; } = string.Empty;

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}