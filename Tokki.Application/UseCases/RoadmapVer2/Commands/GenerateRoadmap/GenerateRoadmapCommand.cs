using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.RoadmapVer2.Commands.GenerateRoadmap
{
    public class GenerateRoadmapCommand : IRequest<OperationResult<string>>
    {
        public TargetAimLevel TargetAim { get; set; } 
        public int DurationDays { get; set; }
        public string UserExamId { get; set; } = string.Empty;
        
        [JsonIgnore]
        public string? JobId { get; set; }

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
