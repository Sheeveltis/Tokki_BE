using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class RoadmapDailyTask
    {
        [Key]
        [MaxLength(15)]
        public string TaskId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string RoadmapWeekId { get; set; } = string.Empty;

        public int DayIndex { get; set; } 

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public RoadmapTaskType TaskType { get; set; }

        public string? AiGeneratedContent { get; set; }

        [MaxLength(10)]
        public string? TargetQuestionTypeId { get; set; }

        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletionDate { get; set; }

        [ForeignKey(nameof(RoadmapWeekId))]
        public virtual RoadmapWeek RoadmapWeek { get; set; } = null!;

        [ForeignKey(nameof(TargetQuestionTypeId))]
        public virtual QuestionType? TargetQuestionType { get; set; }
        public string? GrammarId { get; set; }
        [ForeignKey("GrammarId")]
        public virtual Grammar? Grammar { get; set; }

        public string? QuestionTypeId { get; set; }
        [ForeignKey("QuestionTypeId")]
        public virtual QuestionType? QuestionType { get; set; }

        public string? ExamId { get; set; }
        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }
    }
}