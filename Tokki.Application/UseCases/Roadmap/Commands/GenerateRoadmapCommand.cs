using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapCommand : IRequest<OperationResult<string>>
    {
        public TargetAimLevel TargetAim { get; set; } 
        public int DurationDays { get; set; }
        public List<string> Weaknesses { get; set; } = new List<string>();
        public CurrentTopikLevel CurrentLevel { get; set; } 

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}