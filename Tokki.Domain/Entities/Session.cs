using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần thêm để dùng [ForeignKey]

namespace Tokki.Domain.Entities
{
    public class Session
    {
        [Key]
        [MaxLength(36)] // SỬA: Từ 15 -> 36 để chứa đủ UUID (ví dụ: "550e8400-e29b-41d4-a716-446655440000")
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)] // Khớp với UserId  bên bảng Account
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)] // SỬA: Từ 255 -> 500. Token JWT thường dài hơn 300 ký tự.
        public string RefreshToken { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [MaxLength(45)] // SỬA: Từ 50 -> 45 (Độ dài tối đa của IPv6)
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public DateTime? RevokedAt { get; set; }

        // Relationship
        [ForeignKey(nameof(UserId))] // Chỉ định rõ khóa ngoại
        public virtual Account? Account { get; set; }
    }
}