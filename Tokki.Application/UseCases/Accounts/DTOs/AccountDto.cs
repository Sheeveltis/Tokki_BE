// Tokki.Application/UseCases/Accounts/DTOs/AccountDto.cs
using System;
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
    }
}