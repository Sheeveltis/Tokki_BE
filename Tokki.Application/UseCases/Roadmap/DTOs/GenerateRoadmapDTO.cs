using System.Collections.Generic;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapDTO
    {
        public TargetAimLevel TargetAim { get; set; }
        public int DurationDays { get; set; }

        public List<string> Weaknesses { get; set; } = new List<string>();

        public CurrentTopikLevel CurrentLevel { get; set; }
    }
}