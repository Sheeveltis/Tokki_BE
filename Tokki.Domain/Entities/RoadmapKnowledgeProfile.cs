using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class RoadmapKnowledgeProfile
    {
        [Key]
        [MaxLength(15)]
        public string ProfileId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserRoadmapId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string QuestionTypeId { get; set; } = string.Empty;

        public double MasteryScore { get; set; } 

        public bool IsWeakness { get; set; } = false; 

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserRoadmapId))]
        public virtual UserRoadmap UserRoadmap { get; set; } = null!;

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType QuestionType { get; set; } = null!;
        public int ConsecutiveFailWeeks { get; set; } = 0;
        public int LastEvaluatedWeekIndex { get; set; } = 0;
    }
}