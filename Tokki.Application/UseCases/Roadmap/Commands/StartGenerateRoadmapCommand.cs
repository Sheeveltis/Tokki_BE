using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.StartGenerateRoadmap
{
    public class StartGenerateRoadmapCommand : IRequest<OperationResult<string>>
    {
        public TargetAimLevel TargetAim { get; set; }
        public int DurationDays { get; set; }
        public string UserExamId { get; set; } = string.Empty;

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}