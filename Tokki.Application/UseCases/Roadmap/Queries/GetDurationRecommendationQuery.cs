using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetDurationRecommendation
{
    public class GetDurationRecommendationQuery : IRequest<OperationResult<DurationRecommendationResult>>
    {
        public string UserId { get; set; } = string.Empty;
        public TargetAimLevel TargetAim { get; set; }
        public List<string> WeakQuestionTypeIds { get; set; } = new();
    }

    public class DurationRecommendationResult
    {
        public int TotalWeakTypes { get; set; }
        public List<DurationOption> Options { get; set; } = new();
    }

    public class DurationOption
    {
        public int Days { get; set; }
        public bool Available { get; set; }
        public bool Recommended { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}