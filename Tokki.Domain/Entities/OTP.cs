using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums; 
namespace Tokki.Domain.Entities
{
    public class Otp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] 
        [MaxLength(15)]
        public string OtpId { get; set; } = string.Empty; 


        [MaxLength(15)]
        public string? UserId { get; set; }

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

        [Column(TypeName = "nvarchar(50)")]
        public OtpStatus Status { get; set; } = OtpStatus.Active;

        public DateTime? UsedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [ForeignKey(nameof(UserId))]
        public virtual Account? Account { get; set; }
    }
}