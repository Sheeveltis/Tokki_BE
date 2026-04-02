using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetEntranceFeedback
{
    public class GetEntranceFeedbackQuery : IRequest<OperationResult<EntranceFeedbackResult>>
    {
        public string UserId { get; set; } = string.Empty;
        public string UserExamId { get; set; } = string.Empty;
        public TargetAimLevel TargetAim { get; set; }
        public CurrentTopikLevel SelfDeclaredLevel { get; set; } 
    }

    public class EntranceFeedbackResult
    {
        public string AiFeedback { get; set; } = string.Empty;
        public int TotalWeakTypes { get; set; }
        public int ReadingWeakCount { get; set; }
        public int ListeningWeakCount { get; set; }
        public int WritingWeakCount { get; set; }
        public List<WeakTypeDto> ReadingIssues { get; set; } = new();
        public List<WeakTypeDto> ListeningIssues { get; set; } = new();
        public List<WeakTypeDto> WritingIssues { get; set; } = new();
        public List<EntranceDurationOption> DurationOptions { get; set; } = new();
        public CurrentTopikLevel SuggestedCurrentLevel { get; set; }
        public string SuggestedCurrentLevelName { get; set; } = string.Empty;
    }

    public class WeakTypeDto
    {
        public string QuestionTypeId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class EntranceDurationOption
    {
        public int Days { get; set; }
        public bool Available { get; set; }
        public bool Recommended { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}