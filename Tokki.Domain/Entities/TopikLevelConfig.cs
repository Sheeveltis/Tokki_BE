using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class TopikLevelConfig
    {
        [Key]
        public int TopikLevelConfigID { get; set; }

        [Required]
        public int TargetAimLevel { get; set; }

        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public int PassScore { get; set; }

        [Required]
        public int TotalScore { get; set; }

        [Required]
        public int ExamGroup { get; set; } // Reference to EnumConfig (Value)

        [Required]
        [MaxLength(100)]
        public string ConfigKey { get; set; } = string.Empty;

        public int ListeningMaxQuestions { get; set; }
        public int ListeningMaxScore { get; set; }
        public int ReadingMaxQuestions { get; set; }
        public int ReadingMaxScore { get; set; }
        public int WritingMaxQuestions { get; set; }
        public int WritingMaxScore { get; set; }

        public int TargetListeningQuestions { get; set; }
        public int TargetListeningScore { get; set; }
        public int TargetReadingQuestions { get; set; }
        public int TargetReadingScore { get; set; }
        public int TargetWritingQuestions { get; set; }
        public int TargetWritingScore { get; set; }

        public string? Strategy { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public DateTime? UpdatedAt { get; set; }
    }
}
