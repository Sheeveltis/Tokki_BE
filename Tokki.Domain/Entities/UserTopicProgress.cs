using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    [Table("UserTopicProgress")]
    public class UserTopicProgress
    {
        [Key]
        [StringLength(15)]
        public string UserTopicProgressId { get; set; } = null!;

        [Required]
        [StringLength(15)]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual Account Account { get; set; } = null!;

        [Required]
        [StringLength(15)]
        public string TopicId { get; set; } = null!;

        [ForeignKey(nameof(TopicId))]
        public virtual Topic Topic { get; set; } = null!;

        public bool IsLearned { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    }
}