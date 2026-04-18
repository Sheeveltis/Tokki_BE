using System.Collections.Generic;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class GenerateRoadmapDTO
    {
        public TargetAimLevel TargetAim { get; set; }
        public int DurationDays { get; set; }

        public string UserExamId { get; set; } = string.Empty;
    }
}