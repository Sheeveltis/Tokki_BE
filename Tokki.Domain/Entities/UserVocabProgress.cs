using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class UserVocabProgress
    {
        [Key]
        [StringLength(15)]
        public string UserVocabProgressId { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string VocabularyId { get; set; } = string.Empty;

        public BoxLevel BoxLevel { get; set; } = 0;
        public DateTime NextReviewAt { get; set; }
        public DateTime? LastReviewedAt { get; set; }
        public double IntervalDays { get; set; } = 0;
        public int Streak { get; set; } = 0;
        public bool IsMastered { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual Account Account { get; set; }

        [ForeignKey(nameof(VocabularyId))]
        public virtual Vocabulary Vocabulary { get; set; }
    }
}