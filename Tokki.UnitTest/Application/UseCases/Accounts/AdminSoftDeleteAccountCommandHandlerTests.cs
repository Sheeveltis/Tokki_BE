using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.AdminSoftDeleteAccount;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class AdminSoftDeleteAccountCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static AdminSoftDeleteAccountCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
            => new((accountRepo ?? MockAccountRepository.GetMock()).Object);

        private static Mock<IAccountRepository> BuildRepoWithUser(Account user)
        {
            var m = MockAccountRepository.GetMock();
            m.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);
            m.Setup(x => x.UpdateUserAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        // ═══════════════════════════════════════════════════════════
        // Admin_Soft_Delete_Account_01 | A | Missing AdminUserId → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingAdminUserId_ShouldReturn401()
        {
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = null,
                TargetUserId = "USER-001"
            };

            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Account - Admin Soft Delete", new TestCaseDetail
            {
                FunctionGroup = "Admin Soft Delete Account",
                TestCaseID = "Admin_Soft_Delete_Account_01",
                Description = "AdminUserId is null or empty → unauthorized",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AdminUserId = null", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Admin_Soft_Delete_Account_02 | A | Missing TargetUserId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingTargetUserId_ShouldReturn400()
        {
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "ADMIN-001",
                TargetUserId = "   "
            };

            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Admin Soft Delete", new TestCaseDetail
            {
                FunctionGroup = "Admin Soft Delete Account",
                TestCaseID = "Admin_Soft_Delete_Account_02",
                Description = "TargetUserId is whitespace → bad request",
                ExpectedResult = "Return 400 TargetUserIdRequired",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TargetUserId = whitespace only", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Admin_Soft_Delete_Account_03 | A | Admin tries to disable their own account → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AdminDisablesSelf_ShouldReturn400()
        {
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "ADMIN-001",
                TargetUserId = "ADMIN-001"
            };

            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Admin Soft Delete", new TestCaseDetail
            {
                FunctionGroup = "Admin Soft Delete Account",
                TestCaseID = "Admin_Soft_Delete_Account_03",
                Description = "Admin tries to disable their own account",
                ExpectedResult = "Return 400 CannotDisableSelf",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AdminUserId == TargetUserId", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Admin_Soft_Delete_Account_04 | A | Target user not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TargetUserNotFound_ShouldReturn404()
        {
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "ADMIN-001",
                TargetUserId = "GHOST-999"
            };

            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Admin Soft Delete", new TestCaseDetail
            {
                FunctionGroup = "Admin Soft Delete Account",
                TestCaseID = "Admin_Soft_Delete_Account_04",
                Description = "Target user does not exist in the system",
                ExpectedResult = "Return 404 UserNotFoundById",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Admin_Soft_Delete_Account_05 | A | Target user already Inactive → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TargetAlreadyInactive_ShouldReturn400()
        {
            var user = MockAccountRepository.GetActiveUser();
            user.Status = AccountStatus.Inactive;

            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "ADMIN-001",
                TargetUserId = user.UserId
            };

            var result = await CreateHandler(BuildRepoWithUser(user)).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Admin Soft Delete", new TestCaseDetail
            {
                FunctionGroup = "Admin Soft Delete Account",
                TestCaseID = "Admin_Soft_Delete_Account_05",
                Description = "Target account is already Inactive",
                ExpectedResult = "Return 400 AccountAlreadyInactive",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account.Status = Inactive", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Admin_Soft_Delete_Account_06 | N | Valid request → Status = Inactive, return 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldSetInactiveAndReturn200()
        {
            var user = MockAccountRepository.GetActiveUser();
            Account? captured = null;

            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "ADMIN-001",
                TargetUserId = user.UserId
            };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("Vô hiệu hóa tài khoản của người dùng thành công.");
            captured!.Status.Should().Be(AccountStatus.Inactive);

            QACollector.LogTestCase("Account - Admin Soft Delete", new TestCaseDetail
            {
                FunctionGroup = "Admin Soft Delete Account",
                TestCaseID = "Admin_Soft_Delete_Account_06",
                Description = "Valid admin disables an active user account",
                ExpectedResult = "Status = Inactive, return 200 success message",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "AdminUserId != TargetUserId",
                    "Account.Status = Active",
                    "UpdateUserAsync and SaveChangesAsync called",
                    "Return 200"
                }
            });
        }
    }
}
