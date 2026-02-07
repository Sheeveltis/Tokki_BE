using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần thêm để dùng [ForeignKey]

namespace Tokki.Domain.Entities
{
    public class Session
    {
        [Key]
        [MaxLength(36)]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)] 
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string RefreshToken { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [MaxLength(45)] 
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public DateTime? RevokedAt { get; set; }

        [ForeignKey(nameof(UserId))] 
        public virtual Account? Account { get; set; }
    }
}