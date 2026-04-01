using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetUserProfile;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class GetUserProfileQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetUserProfileQueryHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
            => new((accountRepo ?? MockAccountRepository.GetMock()).Object);

        // ═══════════════════════════════════════════════════════════
        // TC-GUP-01 | A | UserId not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var query = new GetUserProfileQuery("GHOST-999");
            var result = await CreateHandler().Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Get User Profile", new TestCaseDetail
            {
                FunctionGroup   = "Get User Profile",
                TestCaseID      = "TC-GUP-01",
                Description     = "UserId does not exist in the system",
                ExpectedResult  = "Return 404 User.NotFound",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GUP-02 | N | Valid UserId → 200 with profile DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUser_ShouldReturn200WithProfileDto()
        {
            var account = MockAccountRepository.GetActiveUser("USER-001", "alice@tokki.com");
            account.FullName          = "Alice Smith";
            account.PhoneNumber       = "0901234567";
            account.AvatarUrl         = "https://cdn.tokki.com/avatar.png";
            account.TotalXP           = 1200;
            account.AchievedGoalStreak = 10;
            account.MaxStreak         = 25;
            account.Level = (TopicLevel)5;
            account.LastLoginAt       = new DateTime(2025, 3, 28, 10, 0, 0);

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync(account);

            var result = await CreateHandler(mockRepo).Handle(new GetUserProfileQuery("USER-001"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.UserId.Should().Be("USER-001");
            result.Data.Email.Should().Be("alice@tokki.com");
            result.Data.FullName.Should().Be("Alice Smith");
            result.Data.TotalXP.Should().Be(1200);
            result.Data.AchievedGoalStreak.Should().Be(10);
            result.Data.Level.Should().Be(TopicLevel.Level5); 
            QACollector.LogTestCase("Account - Get User Profile", new TestCaseDetail
            {
                FunctionGroup   = "Get User Profile",
                TestCaseID      = "TC-GUP-02",
                Description     = "Valid user ID → 200 with all profile fields mapped",
                ExpectedResult  = "Return 200, DTO fields (UserId, Email, TotalXP, Level, Streak) are correct",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetByIdAsync returns valid account",
                    "All profile fields mapped correctly",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GUP-03 | B | Account with null DateOfBirth → DateOfBirth = null in DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullDateOfBirth_ShouldReturnNullInDto()
        {
            var account = MockAccountRepository.GetActiveUser();
            account.DateOfBirth = null;

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(account);

            var result = await CreateHandler(mockRepo).Handle(new GetUserProfileQuery(account.UserId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.DateOfBirth.Should().BeNull();

            QACollector.LogTestCase("Account - Get User Profile", new TestCaseDetail
            {
                FunctionGroup   = "Get User Profile",
                TestCaseID      = "TC-GUP-03",
                Description     = "Account DateOfBirth is null → DTO.DateOfBirth is null (nullable DateOnly)",
                ExpectedResult  = "Return 200, DateOfBirth = null",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.DateOfBirth = null",
                    "DTO.DateOfBirth = null (not a fallback, truly nullable)"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GUP-04 | N | Account with DateOfBirth set → correctly converted to DateOnly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountWithDateOfBirth_ShouldConvertToDateOnly()
        {
            var account = MockAccountRepository.GetActiveUser();
            account.DateOfBirth = new DateTime(1998, 5, 20, 0, 0, 0);

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(account);

            var result = await CreateHandler(mockRepo).Handle(new GetUserProfileQuery(account.UserId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.DateOfBirth.Should().Be(new DateOnly(1998, 5, 20));

            QACollector.LogTestCase("Account - Get User Profile", new TestCaseDetail
            {
                FunctionGroup   = "Get User Profile",
                TestCaseID      = "TC-GUP-04",
                Description     = "Account has DateOfBirth 1998-05-20 → DTO.DateOfBirth = DateOnly(1998, 5, 20)",
                ExpectedResult  = "Return 200, DateOfBirth = DateOnly(1998, 5, 20)",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.DateOfBirth = DateTime(1998, 5, 20)",
                    "DateOnly.FromDateTime conversion applied",
                    "DTO.DateOfBirth = DateOnly(1998, 5, 20)"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GUP-05 | N | Account without AvatarUrl → AvatarUrl is null in DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoAvatarUrl_ShouldReturnNullAvatarInDto()
        {
            var account = MockAccountRepository.GetActiveUser();
            account.AvatarUrl = null;

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(account);

            var result = await CreateHandler(mockRepo).Handle(new GetUserProfileQuery(account.UserId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.AvatarUrl.Should().BeNull();

            QACollector.LogTestCase("Account - Get User Profile", new TestCaseDetail
            {
                FunctionGroup   = "Get User Profile",
                TestCaseID      = "TC-GUP-05",
                Description     = "Account has no avatar → AvatarUrl is null in DTO",
                ExpectedResult  = "Return 200, AvatarUrl = null",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.AvatarUrl = null",
                    "DTO.AvatarUrl = null",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GUP-06 | N | Admin account profile → Role mapped as Admin
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AdminAccount_ShouldReturnAdminRoleInDto()
        {
            var account = MockAccountRepository.GetAdminUser("ADMIN-001", "admin@tokki.com");
            account.AvatarUrl         = "https://cdn.tokki.com/admin-avatar.png";
            account.TotalXP           = 9999;
            account.MaxStreak         = 100;
            account.LastLoginAt       = new DateTime(2025, 3, 29, 8, 0, 0);

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync("ADMIN-001")).ReturnsAsync(account);

            var result = await CreateHandler(mockRepo).Handle(new GetUserProfileQuery("ADMIN-001"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Role.Should().Be(AccountRole.Admin);
            result.Data.MaxStreak.Should().Be(100);
            result.Data.TotalXP.Should().Be(9999);
            result.Data.LastLoginAt.Should().Be(new DateTime(2025, 3, 29, 8, 0, 0));

            QACollector.LogTestCase("Account - Get User Profile", new TestCaseDetail
            {
                FunctionGroup   = "Get User Profile",
                TestCaseID      = "TC-GUP-06",
                Description     = "Admin account profile → Role = Admin, gamification stats correctly mapped",
                ExpectedResult  = "Return 200, Role = Admin, MaxStreak = 100, TotalXP = 9999, LastLoginAt correct",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.Role = Admin",
                    "TotalXP = 9999, MaxStreak = 100",
                    "LastLoginAt mapped correctly",
                    "Return 200"
                }
            });
        }
    }
}
