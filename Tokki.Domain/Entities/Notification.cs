using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Notification
    {
        [Key]
        [MaxLength(21)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public NotificationType Type { get; set; } = NotificationType.System;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [MaxLength(50)]
        public string? ReferenceId { get; set; }

        [ForeignKey("UserId")]
        public virtual Account Account { get; set; } = default!;
    }
}
