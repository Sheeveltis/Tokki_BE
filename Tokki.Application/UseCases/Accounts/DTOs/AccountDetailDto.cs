using System;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class AccountDetailDto
    {
        // Tất cả các trường trong Entity
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? PasswordHash { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public AccountRole Role { get; set; }
        public AccountStatus Status { get; set; }
        public DateTimeOffset? VipExpirationDate { get; set; }
        public long TotalXP { get; set; }
        public int AchievedGoalStreak { get; set; }
        public int MaxStreak { get; set; }
        public DateTime? LastStreakDate { get; set; }
        public double DailyStudySeconds { get; set; }
        public string? CurrentTitleId { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}