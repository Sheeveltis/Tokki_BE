using System.Collections.Generic;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class AccountTestData
    {
        public static Account GetValidAccount(string email, string password)
        {
            return new Account
            {
                UserId = "User-Test-01",
                Email = email,
                FullName = "Test User Tokki",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Status = AccountStatus.Active,
                Role = AccountRole.User,
                FailedLoginCount = 0
            };
        }

        public static Account GetBannedAccount(string email, string password)
        {
            return new Account
            {
                UserId = "User-Banned",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Status = AccountStatus.Banned,
                Role = AccountRole.User
            };
        }

        public static LoginCommand GetLoginCommand(string email, string password)
        {
            return new LoginCommand
            {
                Email = email,
                Password = password
            };
        }
    }
}