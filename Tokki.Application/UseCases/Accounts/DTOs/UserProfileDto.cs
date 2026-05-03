using System;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class UserProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }

        public DateOnly? DateOfBirth { get; set; } // nullable
        public AccountRole Role { get; set; }
        public AccountStatus Status { get; set; }

        public long TotalXP { get; set; }
        public int AchievedGoalStreak { get; set; }
        public int MaxStreak { get; set; }

        public string? CurrentTitle { get; set; } // tối thiểu: CurrentTitleId
        public int? Level { get; set; } // nullable theo DB bạn thêm
        public int? AimLevel { get; set; } // level mục tiêu
        public DateTime? LastLoginAt { get; set; } // nullable
    }
}
