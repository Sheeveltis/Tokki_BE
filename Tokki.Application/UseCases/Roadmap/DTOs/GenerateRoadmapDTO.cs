using System.Collections.Generic;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapDTO
    {
        public string TargetAim { get; set; } = string.Empty; 
        public int DurationDays { get; set; }

        public List<string> Weaknesses { get; set; } = new List<string>();

        public string CurrentLevel { get; set; } = "Beginner"; 
    }
}