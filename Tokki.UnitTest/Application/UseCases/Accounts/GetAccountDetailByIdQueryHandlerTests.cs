using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetAccountDetailById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class GetAccountDetailByIdQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetAccountDetailByIdQueryHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
            => new((accountRepo ?? MockAccountRepository.GetMock()).Object);

        // ═══════════════════════════════════════════════════════════
        // Get_Account_Detail_By_Id_01 | A | UserId not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var query = new GetAccountDetailByIdQuery { UserId = "GHOST-999" };
            var result = await CreateHandler().Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Get Account Detail By Id", new TestCaseDetail
            {
                FunctionGroup   = "Get Account Detail By Id",
                TestCaseID      = "Get_Account_Detail_By_Id_01",
                Description     = "UserId does not exist in the system",
                ExpectedResult  = "Return 404 AccountNotFound",
                StatusRound1    = "Passed",
                TestCaseType    = "A",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Account_Detail_By_Id_02 | N | Valid UserId → 200, all DTO fields mapped
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUser_ShouldReturn200WithDetailDto()
        {
            var account = MockAccountRepository.GetActiveUser("USER-001", "alice@tokki.com");
            account.FullName          = "Alice Smith";
            account.Role              = AccountRole.Admin;
            account.TotalXP           = 500;
            account.FailedLoginCount  = 2;
            account.LockedUntil       = null;
            account.CreatedAt         = new DateTime(2024, 1, 15);

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync(account);

            var query = new GetAccountDetailByIdQuery { UserId = "USER-001" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.UserId.Should().Be("USER-001");
            result.Data.Email.Should().Be("alice@tokki.com");
            result.Data.Role.Should().Be(AccountRole.Admin);
            result.Data.TotalXP.Should().Be(500);
            result.Data.FailedLoginCount.Should().Be(2);

            QACollector.LogTestCase("Account - Get Account Detail By Id", new TestCaseDetail
            {
                FunctionGroup   = "Get Account Detail By Id",
                TestCaseID      = "Get_Account_Detail_By_Id_02",
                Description     = "Valid UserId → 200 with fully mapped AccountDetailDto",
                ExpectedResult  = "Return 200, all DTO fields (UserId, Email, Role, TotalXP, FailedLoginCount) are correct",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetByIdAsync returns valid account",
                    "All DTO fields mapped correctly",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Account_Detail_By_Id_03 | N | Account with null DateOfBirth → fallback to 2000-01-01
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullDateOfBirth_ShouldFallbackToDefault()
        {
            var account = MockAccountRepository.GetActiveUser();
            account.DateOfBirth = null;

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(account);

            var query = new GetAccountDetailByIdQuery { UserId = account.UserId };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.DateOfBirth.Should().Be(new DateTime(2000, 1, 1));

            QACollector.LogTestCase("Account - Get Account Detail By Id", new TestCaseDetail
            {
                FunctionGroup   = "Get Account Detail By Id",
                TestCaseID      = "Get_Account_Detail_By_Id_03",
                Description     = "Account DateOfBirth is null → DTO falls back to 2000-01-01",
                ExpectedResult  = "Return 200, DateOfBirth = new DateTime(2000, 1, 1)",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.DateOfBirth = null",
                    "DTO.DateOfBirth = new DateTime(2000, 1, 1)"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Account_Detail_By_Id_04 | N | Account with LockedUntil set → DTO reflects the value
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_LockedAccount_ShouldIncludeLockedUntilInDto()
        {
            var lockTime = DateTime.UtcNow.AddHours(7).AddMinutes(30);
            var account = MockAccountRepository.GetActiveUser();
            account.LockedUntil = lockTime;
            account.FailedLoginCount = 5;

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(account);

            var query = new GetAccountDetailByIdQuery { UserId = account.UserId };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.LockedUntil.Should().Be(lockTime);
            result.Data.FailedLoginCount.Should().Be(5);

            QACollector.LogTestCase("Account - Get Account Detail By Id", new TestCaseDetail
            {
                FunctionGroup   = "Get Account Detail By Id",
                TestCaseID      = "Get_Account_Detail_By_Id_04",
                Description     = "Account is temporarily locked → LockedUntil and FailedLoginCount included in DTO",
                ExpectedResult  = "Return 200, LockedUntil is set, FailedLoginCount = 5",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "LockedUntil = now + 30 min",
                    "FailedLoginCount = 5",
                    "DTO correctly reflects these values"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Account_Detail_By_Id_05 | N | Staff account → Role correctly mapped in DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StaffAccount_ShouldReturnCorrectRole()
        {
            var account = MockAccountRepository.GetStaffUser("STAFF-001", "staff@tokki.com");

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync("STAFF-001")).ReturnsAsync(account);

            var query = new GetAccountDetailByIdQuery { UserId = "STAFF-001" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Role.Should().Be(AccountRole.Staff);
            result.Data.Status.Should().Be(AccountStatus.Active);

            QACollector.LogTestCase("Account - Get Account Detail By Id", new TestCaseDetail
            {
                FunctionGroup   = "Get Account Detail By Id",
                TestCaseID      = "Get_Account_Detail_By_Id_05",
                Description     = "Staff account → Role = Staff correctly mapped in DTO",
                ExpectedResult  = "Return 200, Role = Staff, Status = Active",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.Role = Staff",
                    "DTO.Role = Staff",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Account_Detail_By_Id_06 | N | Account with AvatarUrl → URL correctly mapped in DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountWithAvatar_ShouldMapAvatarUrl()
        {
            var account = MockAccountRepository.GetActiveUser();
            account.AvatarUrl = "https://cdn.tokki.com/avatars/user001.png";

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(account.UserId)).ReturnsAsync(account);

            var query = new GetAccountDetailByIdQuery { UserId = account.UserId };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.AvatarUrl.Should().Be("https://cdn.tokki.com/avatars/user001.png");

            QACollector.LogTestCase("Account - Get Account Detail By Id", new TestCaseDetail
            {
                FunctionGroup   = "Get Account Detail By Id",
                TestCaseID      = "Get_Account_Detail_By_Id_06",
                Description     = "Account has an AvatarUrl → correctly mapped to DTO",
                ExpectedResult  = "Return 200, AvatarUrl matches the stored URL",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "AvatarUrl is set on account entity",
                    "DTO.AvatarUrl matches",
                    "Return 200"
                }
            });
        }
    }
}
