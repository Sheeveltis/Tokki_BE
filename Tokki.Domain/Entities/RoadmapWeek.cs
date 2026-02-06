using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class RoadmapWeek
    {
        [Key]
        [MaxLength(15)]
        public string RoadmapWeekId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserRoadmapId { get; set; } = string.Empty;

        public int WeekIndex { get; set; } 

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        [MaxLength(500)]
        public string? WeekFocusGoal { get; set; } 

        public RoadmapWeekStatus Status { get; set; } = RoadmapWeekStatus.Locked;

        [MaxLength(15)]
        public string? WeeklyExamId { get; set; }

        [ForeignKey(nameof(UserRoadmapId))]
        public virtual UserRoadmap UserRoadmap { get; set; } = null!;

        [ForeignKey(nameof(WeeklyExamId))]
        public virtual Exam? WeeklyExam { get; set; }

        public virtual ICollection<RoadmapDailyTask> DailyTasks { get; set; } = new List<RoadmapDailyTask>();
    }
}