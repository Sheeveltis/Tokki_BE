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
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }

        public string? CurrentTitle { get; set; } // tối thiểu: CurrentTitleId
        public TopicLevel? Level { get; set; } // nullable theo DB bạn thêm
        public DateTime? LastLoginAt { get; set; } // nullable
    }
}
