using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.ResetPassword;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class ResetPasswordCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static ResetPasswordCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
            => new(
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                new Mock<IValidator<ResetPasswordCommand>>().Object);

        private static Mock<IAccountRepository> BuildRepoWithUser(Account user)
        {
            var m = MockAccountRepository.GetMock();
            m.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            m.Setup(x => x.UpdateUserAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPW-01 | A | Email not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var result = await CreateHandler().Handle(new ResetPasswordCommand
            {
                Email = "nobody@tokki.com",
                NewPassword = "Reset123!",
                ConfirmPassword = "Reset123!"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Reset Password", new TestCaseDetail
            {
                FunctionGroup     = "Reset Password (Forgot)",
                TestCaseID        = "TC-RPW-01",
                Description       = "Email does not exist in the system",
                ExpectedResult    = "Return 404 UserNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByEmailAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPW-02 | N | Valid email, locked account → reset clears lock, return 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidEmail_LockedAccount_ShouldResetAndClearLock()
        {
            const string newPassword = "NewSecure789!";
            var user = MockAccountRepository.GetActiveUser();
            user.FailedLoginCount = 5;
            user.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(30);

            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(new ResetPasswordCommand
            {
                Email = user.Email,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("Đổi mật khẩu thành công!");
            BCrypt.Net.BCrypt.Verify(newPassword, captured!.PasswordHash).Should().BeTrue();
            captured.FailedLoginCount.Should().Be(0);
            captured.LockedUntil.Should().BeNull();

            QACollector.LogTestCase("Account - Reset Password", new TestCaseDetail
            {
                FunctionGroup     = "Reset Password (Forgot)",
                TestCaseID        = "TC-RPW-02",
                Description       = "Valid email, account was locked → new password hashed, lock cleared, return 200",
                ExpectedResult    = "Return 200, PasswordHash updated, FailedLoginCount = 0, LockedUntil = null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "User found by email",
                    "FailedLoginCount = 5 and LockedUntil set before reset",
                    "New password BCrypt hashed",
                    "FailedLoginCount reset to 0",
                    "LockedUntil reset to null",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPW-03 | N | Valid email, clean account → password changed, return 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidEmail_NormalAccount_ShouldUpdatePasswordAndReturn200()
        {
            const string newPassword = "BrandNew456!";
            var user = MockAccountRepository.GetActiveUser();
            user.FailedLoginCount = 0;
            user.LockedUntil = null;

            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(new ResetPasswordCommand
            {
                Email = user.Email,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            BCrypt.Net.BCrypt.Verify(newPassword, captured!.PasswordHash).Should().BeTrue();

            QACollector.LogTestCase("Account - Reset Password", new TestCaseDetail
            {
                FunctionGroup     = "Reset Password (Forgot)",
                TestCaseID        = "TC-RPW-03",
                Description       = "Valid email, account has no lock → password resets successfully",
                ExpectedResult    = "Return 200, PasswordHash updated with new password",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "User found, FailedLoginCount = 0, LockedUntil = null",
                    "New password BCrypt hashed",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPW-04 | N | UpdateUserAsync and SaveChangesAsync each called exactly once
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidEmail_ShouldCallUpdateUserAsyncOnce()
        {
            var user = MockAccountRepository.GetActiveUser();
            var mockRepo = BuildRepoWithUser(user);

            await CreateHandler(mockRepo).Handle(new ResetPasswordCommand
            {
                Email = user.Email,
                NewPassword = "NewPass999!",
                ConfirmPassword = "NewPass999!"
            }, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Account - Reset Password", new TestCaseDetail
            {
                FunctionGroup     = "Reset Password (Forgot)",
                TestCaseID        = "TC-RPW-04",
                Description       = "Valid email → UpdateUserAsync and SaveChangesAsync each called exactly once",
                ExpectedResult    = "UpdateUserAsync called 1 time, SaveChangesAsync called 1 time",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "User found",
                    "UpdateUserAsync called × 1",
                    "SaveChangesAsync called × 1"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPW-05 | B | Old password hash is completely replaced by new hash
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidEmail_ShouldNotVerifyWithOldPassword()
        {
            const string oldPassword = "OldPass123!";
            const string newPassword = "FreshPass999!";

            var user = MockAccountRepository.GetActiveUser("USER-001", "alice@tokki.com");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            var originalHash = user.PasswordHash;

            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            await CreateHandler(mockRepo).Handle(new ResetPasswordCommand
            {
                Email = user.Email,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            }, CancellationToken.None);

            // Old password must NOT verify against the new hash
            BCrypt.Net.BCrypt.Verify(oldPassword, captured!.PasswordHash).Should().BeFalse();
            // New password must verify
            BCrypt.Net.BCrypt.Verify(newPassword, captured.PasswordHash).Should().BeTrue();
            // Hash must have changed
            captured.PasswordHash.Should().NotBe(originalHash);

            QACollector.LogTestCase("Account - Reset Password", new TestCaseDetail
            {
                FunctionGroup     = "Reset Password (Forgot)",
                TestCaseID        = "TC-RPW-05",
                Description       = "After reset, old password no longer valid; new hash is different from old one",
                ExpectedResult    = "Old password BCrypt.Verify = false, new password BCrypt.Verify = true, hash changed",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "OldPassword → BCrypt.Verify = false on new hash",
                    "NewPassword → BCrypt.Verify = true on new hash",
                    "PasswordHash changed from original"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPW-06 | N | Admin account can also reset password via this handler → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AdminAccount_ShouldResetPasswordAndReturn200()
        {
            const string newPassword = "AdminNew456!";
            var admin = MockAccountRepository.GetAdminUser();
            admin.FailedLoginCount = 2;
            admin.LockedUntil = null;

            Account? captured = null;
            var mockRepo = BuildRepoWithUser(admin);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(new ResetPasswordCommand
            {
                Email = admin.Email,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            captured!.FailedLoginCount.Should().Be(0);
            BCrypt.Net.BCrypt.Verify(newPassword, captured.PasswordHash).Should().BeTrue();

            QACollector.LogTestCase("Account - Reset Password", new TestCaseDetail
            {
                FunctionGroup     = "Reset Password (Forgot)",
                TestCaseID        = "TC-RPW-06",
                Description       = "Admin account password reset works the same as a regular user",
                ExpectedResult    = "Return 200, Admin's PasswordHash updated, FailedLoginCount reset to 0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.Role = Admin",
                    "New password hashed correctly",
                    "FailedLoginCount reset to 0",
                    "Return 200"
                }
            });
        }
    }
}
