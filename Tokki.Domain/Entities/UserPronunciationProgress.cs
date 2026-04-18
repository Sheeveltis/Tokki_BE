using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    [Table("UserPronunciationProgress")]
    public class UserPronunciationProgress
    {
        [Key]
        [Column(TypeName = "varchar(15)")]
        public string UserPronunciationProgressId { get; set; } = null!;

        [Required]
        [Column(TypeName = "nvarchar(15)")]
        public string UserId { get; set; } = null!; // FK to Accounts

        [ForeignKey(nameof(UserId))]
        public virtual Account Account { get; set; } = null!;

        [Required]
        [Column(TypeName = "varchar(10)")]
        public string PronunciationRuleId { get; set; } = null!; // FK to PronunciationRules

        [ForeignKey(nameof(PronunciationRuleId))]
        public virtual PronunciationRule PronunciationRule { get; set; } = null!;

        public bool IsLearned { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    }
}
