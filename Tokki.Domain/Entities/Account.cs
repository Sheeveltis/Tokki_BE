using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    [Index(nameof(Email), IsUnique = true)] // Đảm bảo Email duy nhất
    public class Account
    {
        [Key]
        [MaxLength(15)] // Nếu dùng NanoID
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; } // Optional: Không bắt buộc

        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }
        // PasswordHash vẫn phải để NULLABLE để hỗ trợ Login Google/Facebook
        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [Required]
        public AccountRole Role { get; set; } = AccountRole.User;

        [Required]
        public AccountStatus Status { get; set; } = AccountStatus.Active;

        public DateTimeOffset? VipExpirationDate { get; set; }

        public long TotalXP { get; set; } = 0; 

        public int CurrentStreak { get; set; } = 0; 
        public int MaxStreak { get; set; } = 0;
        public DateTime? LastStreakDate { get; set; }
        public double DailyStudySeconds { get; set; } = 0;
        [MaxLength(21)]
        public string? CurrentTitleId { get; set; } 

        [ForeignKey("CurrentTitleId")]
        public virtual Title? CurrentTitle { get; set; }
        public virtual ICollection<AccountTitle> UnlockedTitles { get; set; } = new List<AccountTitle>();

        // --- MỚI THÊM LẠI (Bảo mật & Tracking) ---

        [Required]
        public int FailedLoginCount { get; set; } = 0; // Mặc định là 0

        public DateTime? LockedUntil { get; set; } // Nullable: Chỉ có giá trị khi bị khóa

        public DateTime? LastLoginAt { get; set; } // Nullable: Lần đầu tạo acc thì chưa login

        // -----------------------------------------

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [Required]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        // Navigation Properties
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<SocialLogin> SocialLogins { get; set; } = new List<SocialLogin>();

           }
}