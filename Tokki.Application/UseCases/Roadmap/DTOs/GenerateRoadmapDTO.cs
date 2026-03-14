using System.Collections.Generic;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapDTO
    {
        public TargetAimLevel TargetAim { get; set; }
        public int DurationDays { get; set; }

        public string UserExamId { get; set; } = string.Empty;

        public CurrentTopikLevel CurrentLevel { get; set; }
    }
}