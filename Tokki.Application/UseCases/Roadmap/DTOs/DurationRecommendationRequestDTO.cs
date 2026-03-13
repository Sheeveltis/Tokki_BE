using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class DurationRecommendationRequestDto
    {
        public string ExamId { get; set; } = string.Empty;
        public TargetAimLevel TargetAim { get; set; }
        public List<string> WeakTypeIds { get; set; } = new();
    }
}