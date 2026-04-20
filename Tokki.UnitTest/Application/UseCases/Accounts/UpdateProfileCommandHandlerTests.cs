using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.UpdateProfile;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class UpdateProfileCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static UpdateProfileCommandHandler CreateHandler(
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

        // ═══════════════════════════════════════════════════════════
        // Update_Profile_01 | A | UserId is null → 401 Unauthorized
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoUserId_ShouldReturn401()
        {
            var result = await CreateHandler().Handle(new UpdateProfileCommand
            {
                UserId   = null,
                FullName = "Test"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Account - Update Profile", new TestCaseDetail
            {
                FunctionGroup     = "Update Profile",
                TestCaseID        = "Update_Profile_01",
                Description       = "UserId is null or empty → cannot identify which user to update",
                ExpectedResult    = "Return 401 UserUnauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId = null", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Profile_02 | A | User not found by ID → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var result = await CreateHandler().Handle(new UpdateProfileCommand
            {
                UserId   = "GHOST-999",
                FullName = "Test"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Update Profile", new TestCaseDetail
            {
                FunctionGroup     = "Update Profile",
                TestCaseID        = "Update_Profile_02",
                Description       = "User ID does not exist in the system",
                ExpectedResult    = "Return 404 UserNotFoundById",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Profile_03 | A | Phone already used by another user → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicatePhone_ShouldReturn400()
        {
            var user     = MockAccountRepository.GetActiveUser();
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.IsPhoneNumberUsedByOtherUserAsync("0901111111", user.UserId))
                    .ReturnsAsync(true);

            var result = await CreateHandler(mockRepo).Handle(new UpdateProfileCommand
            {
                UserId      = user.UserId,
                FullName    = "Same Name",
                PhoneNumber = "0901111111"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Update Profile", new TestCaseDetail
            {
                FunctionGroup     = "Update Profile",
                TestCaseID        = "Update_Profile_03",
                Description       = "Phone number is already used by another account",
                ExpectedResult    = "Return 400 PhoneNumberDuplicated",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "PhoneNumber provided",
                    "IsPhoneNumberUsedByOtherUserAsync = true",
                    "Return 400"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Profile_04 | N | Valid full profile update (all fields) → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidFullUpdate_ShouldReturn200()
        {
            var user = MockAccountRepository.GetActiveUser();
            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(new UpdateProfileCommand
            {
                UserId      = user.UserId,
                FullName    = "Updated Full Name",
                PhoneNumber = "0909888777",
                DateOfBirth = new DateOnly(1998, 5, 20),
                AvatarUrl   = "https://cdn.tokki.com/avatar.png"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("Cập nhật thông tin thành công!");
            captured!.FullName.Should().Be("Updated Full Name");
            captured.PhoneNumber.Should().Be("0909888777");
            captured.AvatarUrl.Should().Be("https://cdn.tokki.com/avatar.png");

            QACollector.LogTestCase("Account - Update Profile", new TestCaseDetail
            {
                FunctionGroup     = "Update Profile",
                TestCaseID        = "Update_Profile_04",
                Description       = "Valid update of FullName, Phone, DateOfBirth, AvatarUrl → all fields updated",
                ExpectedResult    = "Return 200, all provided fields updated on entity",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "User found",
                    "Phone not duplicate",
                    "All provided fields updated",
                    "UpdateUserAsync called",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Profile_05 | N | Only FullName provided → phone logic skipped, return 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OnlyFullNameProvided_ShouldReturn200()
        {
            var user = MockAccountRepository.GetActiveUser();
            Account? captured = null;
            var mockRepo = BuildRepoWithUser(user);
            mockRepo.Setup(x => x.UpdateUserAsync(It.IsAny<Account>()))
                    .Callback<Account>(a => captured = a)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(new UpdateProfileCommand
            {
                UserId   = user.UserId,
                FullName = "Only Name Changed"
            }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            captured!.FullName.Should().Be("Only Name Changed");

            QACollector.LogTestCase("Account - Update Profile", new TestCaseDetail
            {
                FunctionGroup     = "Update Profile",
                TestCaseID        = "Update_Profile_05",
                Description       = "Only FullName provided, phone check and phone update are skipped",
                ExpectedResult    = "Return 200, only FullName updated",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "FullName provided",
                    "PhoneNumber = null → phone logic skipped",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Profile_06 | N | UpdateUserAsync and SaveChangesAsync each called once on success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUpdate_ShouldCallUpdateAndSaveOnce()
        {
            var user     = MockAccountRepository.GetActiveUser();
            var mockRepo = BuildRepoWithUser(user);

            await CreateHandler(mockRepo).Handle(new UpdateProfileCommand
            {
                UserId   = user.UserId,
                FullName = "Save Verified"
            }, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Account - Update Profile", new TestCaseDetail
            {
                FunctionGroup     = "Update Profile",
                TestCaseID        = "Update_Profile_06",
                Description       = "Successful update → UpdateUserAsync and SaveChangesAsync each called exactly once",
                ExpectedResult    = "UpdateUserAsync × 1, SaveChangesAsync × 1",
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
    }
}
