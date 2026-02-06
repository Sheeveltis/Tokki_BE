using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class UserRoadmap
    {
        [Key]
        [MaxLength(15)]
        public string UserRoadmapId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string TargetAim { get; set; } = string.Empty; 

        public int DurationDays { get; set; } 

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public UserRoadmapStatus CurrentStatus { get; set; } = UserRoadmapStatus.Active;

        public string? OverallAiAssessment { get; set; }

        public string? SystemPromptContext { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual Account Account { get; set; } = null!;

        public virtual ICollection<RoadmapWeek> Weeks { get; set; } = new List<RoadmapWeek>();
        public virtual ICollection<RoadmapKnowledgeProfile> KnowledgeProfiles { get; set; } = new List<RoadmapKnowledgeProfile>();
    }
}