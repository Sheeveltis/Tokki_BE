using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.ChangePassword;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class ChangePasswordCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static ChangePasswordCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
            => new((accountRepo ?? MockAccountRepository.GetMock()).Object);

        private static Mock<IAccountRepository> BuildRepoWithUser(Account user)
        {
            var m = MockAccountRepository.GetMock();
            m.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            m.Setup(x => x.UpdateUserAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CPW-01 | A | Email not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var result = await CreateHandler().Handle(new ChangePasswordCommand
            {
                Email = "ghost@tokki.com",
                OldPassword = "OldPass123!",
                NewPassword = "NewPass123!"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Change Password", new TestCaseDetail
            {
                FunctionGroup     = "Change Password",
                TestCaseID        = "TC-CPW-01",
                Description       = "Email does not exist in the system",
                ExpectedResult    = "Return 404 UserNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByEmailAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CPW-02 | A | Wrong old password → 400 InvalidCredentials
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongOldPassword_ShouldReturn400()
        {
            var user = MockAccountRepository.GetActiveUser(); // hashed "ValidPass123!"

            var result = await CreateHandler(BuildRepoWithUser(user)).Handle(new ChangePasswordCommand
            {
                Email = user.Email,
                OldPassword = "WrongOldPassword!",
                NewPassword = "NewPass123!"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Change Password", new TestCaseDetail
            {
                FunctionGroup     = "Change Password",
                TestCaseID        = "TC-CPW-02",
                Description       = "Old password does not match the stored BCrypt hash",
                ExpectedResult    = "Return 400 InvalidCredentials",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "BCrypt.Verify = false", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CPW-03 | N | Correct old password → new hash set, lock reset, return 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CorrectOldPassword_ShouldUpdateHashAndReturn200()
        {
            const string oldPassword = "ValidPass123!";
            const string newPassword = "BrandNew456!";

            var user = MockAccountRepository.GetActiveUser();
            user.PasswordHash       = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            user.FailedLoginCount   = 3;
            user.LockedUntil        = DateTime.UtcNow.AddHours(7).AddMinutes(10);

            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(new ChangePasswordCommand
            {
                Email = user.Email,
                OldPassword = oldPassword,
                NewPassword = newPassword
            }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("Đổi mật khẩu thành công!");
            BCrypt.Net.BCrypt.Verify(newPassword, captured!.PasswordHash).Should().BeTrue();
            captured.FailedLoginCount.Should().Be(0);
            captured.LockedUntil.Should().BeNull();

            QACollector.LogTestCase("Account - Change Password", new TestCaseDetail
            {
                FunctionGroup     = "Change Password",
                TestCaseID        = "TC-CPW-03",
                Description       = "Correct old password → new hash, FailedLoginCount and LockedUntil reset",
                ExpectedResult    = "Return 200, new hash set, FailedLoginCount = 0, LockedUntil = null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "BCrypt.Verify = true",
                    "New password hashed",
                    "FailedLoginCount reset to 0",
                    "LockedUntil reset to null",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CPW-04 | N | UpdateUserAsync called exactly once on success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Success_ShouldCallUpdateAndSaveOnce()
        {
            const string oldPassword = "ValidPass123!";
            var user = MockAccountRepository.GetActiveUser();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);

            var mockRepo = BuildRepoWithUser(user);

            await CreateHandler(mockRepo).Handle(new ChangePasswordCommand
            {
                Email = user.Email,
                OldPassword = oldPassword,
                NewPassword = "NewPassed789!"
            }, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Account - Change Password", new TestCaseDetail
            {
                FunctionGroup     = "Change Password",
                TestCaseID        = "TC-CPW-04",
                Description       = "Successful change → UpdateUserAsync and SaveChangesAsync each called exactly once",
                ExpectedResult    = "UpdateUserAsync × 1, SaveChangesAsync × 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "BCrypt.Verify = true",
                    "UpdateUserAsync called × 1",
                    "SaveChangesAsync called × 1"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CPW-05 | B | Old password must NOT verify with the new hash
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Success_OldPasswordShouldNotMatchNewHash()
        {
            const string oldPassword = "ValidPass123!";
            const string newPassword = "CompletelyNew!";

            var user = MockAccountRepository.GetActiveUser();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);

            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            await CreateHandler(mockRepo).Handle(new ChangePasswordCommand
            {
                Email = user.Email,
                OldPassword = oldPassword,
                NewPassword = newPassword
            }, CancellationToken.None);

            BCrypt.Net.BCrypt.Verify(oldPassword, captured!.PasswordHash).Should().BeFalse();
            BCrypt.Net.BCrypt.Verify(newPassword, captured.PasswordHash).Should().BeTrue();

            QACollector.LogTestCase("Account - Change Password", new TestCaseDetail
            {
                FunctionGroup     = "Change Password",
                TestCaseID        = "TC-CPW-05",
                Description       = "After change, old password must fail BCrypt.Verify; new password must pass",
                ExpectedResult    = "Old password BCrypt.Verify = false, New password BCrypt.Verify = true",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "OldPassword → BCrypt.Verify = false on new hash",
                    "NewPassword → BCrypt.Verify = true on new hash"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CPW-06 | A | Wrong old password → UpdateUserAsync NOT called
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongOldPassword_ShouldNotCallUpdate()
        {
            var user = MockAccountRepository.GetActiveUser(); // hashed "ValidPass123!"
            var mockRepo = BuildRepoWithUser(user);

            await CreateHandler(mockRepo).Handle(new ChangePasswordCommand
            {
                Email = user.Email,
                OldPassword = "WrongPass!",
                NewPassword = "NewPass789!"
            }, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Never);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            QACollector.LogTestCase("Account - Change Password", new TestCaseDetail
            {
                FunctionGroup     = "Change Password",
                TestCaseID        = "TC-CPW-06",
                Description       = "Wrong old password → no update or save should be called",
                ExpectedResult    = "UpdateUserAsync × 0, SaveChangesAsync × 0",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "BCrypt.Verify = false",
                    "UpdateUserAsync NOT called",
                    "SaveChangesAsync NOT called"
                }
            });
        }
    }
}
