using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockAccountRepository
    {
        public static Mock<IAccountRepository> GetMock(
            List<Account>? existingAccounts = null,
            List<string>? existingEmails = null)
        {
            var mockRepo = new Mock<IAccountRepository>();

            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingAccounts ?? new List<Account>());

            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingEmails ?? new List<string>());

            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Account>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.AddAsync(It.IsAny<Account>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

            mockRepo.Setup(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

            mockRepo.Setup(x => x.IsPhoneNumberUsedByOtherUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(false);

            mockRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync((Account?)null);

            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((Account?)null);

            mockRepo.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>()))
                    .ReturnsAsync((string userId) => 
                    {
                        var user = (existingAccounts ?? new List<Account>()).FirstOrDefault(u => u.UserId == userId);
                        if (user == null) return null;
                        return new Tokki.Application.UseCases.Accounts.DTOs.AccountBasicInfoDTO
                        {
                            FullName = user.FullName,
                            AvatarUrl = user.AvatarUrl ?? "default-avatar.png"
                        };
                    });

            return mockRepo;
        }

        // ── Sample account builders ───────────────────────────────────

        public static Account GetActiveUser(string userId = "USER-001", string email = "user@tokki.com") => new()
        {
            UserId = userId,
            Email = email,
            FullName = "Test User",
            Role = AccountRole.User,
            Status = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPass123!"),
            FailedLoginCount = 0,
            PhoneNumber = "0901234567",
            DateOfBirth = new DateTime(2000, 1, 1)
        };

        public static Account GetAdminUser(string userId = "ADMIN-001", string email = "admin@tokki.com") => new()
        {
            UserId = userId,
            Email = email,
            FullName = "Admin User",
            Role = AccountRole.Admin,
            Status = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123!"),
            FailedLoginCount = 0
        };

        public static Account GetStaffUser(string userId = "STAFF-001", string email = "staff@tokki.com") => new()
        {
            UserId = userId,
            Email = email,
            FullName = "Staff User",
            Role = AccountRole.Staff,
            Status = AccountStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StaffPass123!"),
            FailedLoginCount = 0
        };

        public static List<Account> GetSampleAccountsForExport() => new()
        {
            new Account { FullName = "Alice Smith",  Email = "alice@tokki.com",  Role = AccountRole.User,  PhoneNumber = "0901234567" },
            new Account { FullName = "Bob Johnson",  Email = "bob@tokki.com",    Role = AccountRole.Admin, PhoneNumber = "0987654321" }
        };
    }
}