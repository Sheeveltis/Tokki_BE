using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums; // Nhớ using namespace chứa Enum

namespace Tokki.Domain.Entities
{
    public class Otp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OtpId { get; set; }

        [MaxLength(15)]
        public string? UserId { get; set; } // Đã khớp với bảng Account mới

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        public string OtpCode { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiredAt { get; set; }

        [Required]
        public int AttemptCount { get; set; } = 0;

        // --- THAY ĐỔI Ở ĐÂY ---
        [Required]
        public OtpType Type { get; set; } = OtpType.VerifyEmail;
        // ---------------------

        [Required]
        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual Account? Account { get; set; }
    }
}