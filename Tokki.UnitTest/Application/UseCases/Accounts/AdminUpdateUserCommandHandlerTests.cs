using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.AdminUpdateUser;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class AdminUpdateUserCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static AdminUpdateUserCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
            => new((accountRepo ?? MockAccountRepository.GetMock()).Object);

        private static Mock<IAccountRepository> BuildRepoWithUser(Account user)
        {
            var m = MockAccountRepository.GetMock();
            m.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);
            m.Setup(x => x.UpdateUserAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            m.Setup(x => x.IsPhoneNumberUsedByOtherUserAsync(It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(false);
            return m;
        }

        private static AdminUpdateUserCommand BuildCommand(
            string targetUserId  = "USER-001",
            string fullName      = "Updated Name",
            AccountRole role     = AccountRole.User,
            AccountStatus status = AccountStatus.Active,
            string adminId       = "ADMIN-001",
            string? phone        = null)
            => new()
            {
                AdminId      = adminId,
                TargetUserId = targetUserId,
                FullName     = fullName,
                Role         = role,
                Status       = status,
                PhoneNumber  = phone
            };

        // ═══════════════════════════════════════════════════════════
        // TC-AUU-01 | A | Target user not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TargetUserNotFound_ShouldReturn404()
        {
            var command = BuildCommand(targetUserId: "GHOST-999");
            var result  = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Admin Update User", new TestCaseDetail
            {
                FunctionGroup     = "Admin Update User",
                TestCaseID        = "TC-AUU-01",
                Description       = "Target user ID does not exist in the system",
                ExpectedResult    = "Return 404 UserNotFoundById",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AUU-02 | A | Duplicate phone used by another user → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicatePhoneByOtherUser_ShouldReturn400()
        {
            var user     = MockAccountRepository.GetActiveUser();
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.IsPhoneNumberUsedByOtherUserAsync("0901111111", user.UserId))
                    .ReturnsAsync(true);

            var command = BuildCommand(targetUserId: user.UserId, phone: "0901111111");
            var result  = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Admin Update User", new TestCaseDetail
            {
                FunctionGroup     = "Admin Update User",
                TestCaseID        = "TC-AUU-02",
                Description       = "New phone number is already used by another user",
                ExpectedResult    = "Return 400 PhoneNumberDuplicated",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "New phone differs from current phone",
                    "IsPhoneNumberUsedByOtherUserAsync = true",
                    "Return 400"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AUU-03 | A | Trying to set Status = Inactive → 400 (invalid transition)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusSetToInactive_ShouldReturn400()
        {
            var user    = MockAccountRepository.GetActiveUser();
            var command = BuildCommand(targetUserId: user.UserId, status: AccountStatus.Inactive);
            var result  = await CreateHandler(BuildRepoWithUser(user)).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Admin Update User", new TestCaseDetail
            {
                FunctionGroup     = "Admin Update User",
                TestCaseID        = "TC-AUU-03",
                Description       = "Setting Status=Inactive via update is forbidden; use soft-delete endpoint",
                ExpectedResult    = "Return 400 AccountInvalidStatusTransition",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Status = Inactive", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AUU-04 | N | Valid update (FullName + Role, no phone change) → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUpdateNoPhoneChange_ShouldReturn200()
        {
            var user     = MockAccountRepository.GetActiveUser();
            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var command = BuildCommand(
                targetUserId: user.UserId,
                fullName: "Alice Updated",
                role: AccountRole.Staff,
                status: AccountStatus.Active);

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            captured!.FullName.Should().Be("Alice Updated");
            captured.Role.Should().Be(AccountRole.Staff);

            QACollector.LogTestCase("Account - Admin Update User", new TestCaseDetail
            {
                FunctionGroup     = "Admin Update User",
                TestCaseID        = "TC-AUU-04",
                Description       = "Valid update of FullName and Role without changing phone",
                ExpectedResult    = "Return 200, FullName = 'Alice Updated', Role = Staff",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "User found",
                    "Status != Inactive",
                    "No phone change",
                    "UpdateUserAsync called",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AUU-05 | N | Valid update with new unique phone → 200, phone updated
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUpdateWithNewPhone_ShouldReturn200()
        {
            var user = MockAccountRepository.GetActiveUser();
            user.PhoneNumber = "0900000001";
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.IsPhoneNumberUsedByOtherUserAsync("0900000099", user.UserId))
                    .ReturnsAsync(false);

            Account? captured = null;
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var command = BuildCommand(targetUserId: user.UserId, phone: "0900000099");
            var result  = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            captured!.PhoneNumber.Should().Be("0900000099");

            QACollector.LogTestCase("Account - Admin Update User", new TestCaseDetail
            {
                FunctionGroup     = "Admin Update User",
                TestCaseID        = "TC-AUU-05",
                Description       = "Valid update with a new unique phone number → phone updated on entity",
                ExpectedResult    = "Return 200, PhoneNumber = '0900000099'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "New phone differs from current phone",
                    "IsPhoneNumberUsedByOtherUserAsync = false",
                    "Phone updated on entity",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-AUU-06 | N | UpdateUserAsync and SaveChangesAsync each called once on success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUpdate_ShouldCallUpdateAndSaveOnce()
        {
            var user     = MockAccountRepository.GetActiveUser();
            var mockRepo = BuildRepoWithUser(user);
            var command  = BuildCommand(targetUserId: user.UserId, fullName: "Verified Save", status: AccountStatus.Active);

            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Account - Admin Update User", new TestCaseDetail
            {
                FunctionGroup     = "Admin Update User",
                TestCaseID        = "TC-AUU-06",
                Description       = "Successful update → UpdateUserAsync and SaveChangesAsync each called exactly once",
                ExpectedResult    = "UpdateUserAsync × 1, SaveChangesAsync × 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "User found, Status != Inactive",
                    "UpdateUserAsync called × 1",
                    "SaveChangesAsync called × 1"
                }
            });
        }
    }
}
