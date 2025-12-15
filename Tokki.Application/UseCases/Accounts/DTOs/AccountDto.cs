using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class AccountDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public AccountRole Role { get; set; }
        public AccountStatus Status { get; set; }
        public DateTimeOffset? VipExpirationDate { get; set; }
        public long TotalXP { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public DateTime? LastStreakDate { get; set; }
        public double DailyStudySeconds { get; set; }

        // Current Title Info
        public string? CurrentTitleId { get; set; }
        public string? CurrentTitleName { get; set; }

        // Security & Tracking
        public int FailedLoginCount { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Statistics
        public int UnlockedTitlesCount { get; set; }
        public int SessionsCount { get; set; }
        public int SocialLoginsCount { get; set; }
    }
}
