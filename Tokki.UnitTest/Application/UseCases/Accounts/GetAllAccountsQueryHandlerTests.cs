using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Accounts.Queries.GetAccount;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class GetAllAccountsQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetAllAccountsQueryHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
            => new((accountRepo ?? MockAccountRepository.GetMock()).Object);

        private static Mock<IAccountRepository> BuildPagedRepoMock(
            List<Account> items,
            int totalCount)
        {
            var m = MockAccountRepository.GetMock();
            m.Setup(x => x.GetPagedWithSearchAsync(
                        It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string?>(), It.IsAny<AccountStatus?>(),
                        It.IsAny<AccountRole?>(), It.IsAny<VipStatus?>()))
             .ReturnsAsync((items.AsEnumerable(), totalCount));
            return m;
        }

        private static List<Account> BuildAccounts(int count)
            => Enumerable.Range(1, count).Select(i => new Account
            {
                UserId      = $"USER-{i:D3}",
                Email       = $"user{i}@tokki.com",
                FullName    = $"User {i}",
                Role        = AccountRole.User,
                Status      = AccountStatus.Active,
                PhoneNumber = $"090000{i:D4}"
            }).ToList();

        // ═══════════════════════════════════════════════════════════
        // TC-GAA-01 | N | No filters, page 1 → returns first page items
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoFilters_Page1_ShouldReturnFirstPage()
        {
            var accounts = BuildAccounts(15);
            var mockRepo = BuildPagedRepoMock(accounts.Take(10).ToList(), 15);

            var query = new GetAllAccountsQuery { PageNumber = 1, PageSize = 10 };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().HaveCount(10);
            result.Data.TotalCount.Should().Be(15);
            result.Data.PageNumber.Should().Be(1);

            QACollector.LogTestCase("Account - Get All Accounts", new TestCaseDetail
            {
                FunctionGroup   = "Get All Accounts",
                TestCaseID      = "TC-GAA-01",
                Description     = "No filters applied, page 1, page size 10 → returns first 10 of 15 records",
                ExpectedResult  = "Return 200, Items.Count = 10, TotalCount = 15, PageNumber = 1",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No filters",
                    "PageNumber = 1, PageSize = 10",
                    "TotalCount = 15",
                    "Return 200"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAA-02 | N | Page 2 → returns remaining items
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Page2_ShouldReturnRemainingItems()
        {
            var remaining = BuildAccounts(5);
            var mockRepo = BuildPagedRepoMock(remaining, 15);

            var query = new GetAllAccountsQuery { PageNumber = 2, PageSize = 10 };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(5);
            result.Data.TotalCount.Should().Be(15);
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("Account - Get All Accounts", new TestCaseDetail
            {
                FunctionGroup   = "Get All Accounts",
                TestCaseID      = "TC-GAA-02",
                Description     = "Page 2 of total 15 records with page size 10 → returns last 5 items",
                ExpectedResult  = "Return 200, Items.Count = 5, TotalCount = 15, PageNumber = 2",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "PageNumber = 2, PageSize = 10",
                    "TotalCount = 15",
                    "5 remaining items on page 2"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAA-03 | N | Empty database → returns empty page
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyDatabase_ShouldReturnEmptyPage()
        {
            var mockRepo = BuildPagedRepoMock(new List<Account>(), 0);
            var query = new GetAllAccountsQuery { PageNumber = 1, PageSize = 10 };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Account - Get All Accounts", new TestCaseDetail
            {
                FunctionGroup   = "Get All Accounts",
                TestCaseID      = "TC-GAA-03",
                Description     = "No accounts in the system → empty result",
                ExpectedResult  = "Return 200, Items empty, TotalCount = 0",
                StatusRound1    = "Passed",
                TestCaseType    = "B",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedWithSearchAsync returns empty list, 0", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAA-04 | N | Filter by Role = Admin → returns only admin accounts
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByRole_ShouldReturnMatchingRoleOnly()
        {
            var adminAccounts = new List<Account>
            {
                MockAccountRepository.GetAdminUser("ADMIN-001"),
                MockAccountRepository.GetAdminUser("ADMIN-002", "admin2@tokki.com")
            };
            var mockRepo = BuildPagedRepoMock(adminAccounts, 2);

            var query = new GetAllAccountsQuery { PageNumber = 1, PageSize = 10, Role = AccountRole.Admin };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().OnlyContain(a => a.Role == AccountRole.Admin);

            QACollector.LogTestCase("Account - Get All Accounts", new TestCaseDetail
            {
                FunctionGroup   = "Get All Accounts",
                TestCaseID      = "TC-GAA-04",
                Description     = "Filter by Role = Admin → only Admin accounts returned",
                ExpectedResult  = "Return 200, all items have Role = Admin",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Role filter = Admin",
                    "Mock returns 2 admin accounts",
                    "All returned items have Role = Admin"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAA-05 | N | Filter by Status = Inactive → returns only inactive accounts
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByStatus_ShouldReturnMatchingStatusOnly()
        {
            var inactiveAccounts = new List<Account>
            {
                new() { UserId = "U1", Email = "inactive1@tokki.com", FullName = "Inactive 1",
                         Role = AccountRole.User, Status = AccountStatus.Inactive }
            };
            var mockRepo = BuildPagedRepoMock(inactiveAccounts, 1);

            var query = new GetAllAccountsQuery { PageNumber = 1, PageSize = 10, Status = AccountStatus.Inactive };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.Should().OnlyContain(a => a.Status == AccountStatus.Inactive);

            QACollector.LogTestCase("Account - Get All Accounts", new TestCaseDetail
            {
                FunctionGroup   = "Get All Accounts",
                TestCaseID      = "TC-GAA-05",
                Description     = "Filter by Status = Inactive → only inactive accounts returned",
                ExpectedResult  = "Return 200, all items have Status = Inactive",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status filter = Inactive",
                    "Mock returns 1 inactive account",
                    "All returned items have Status = Inactive"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-GAA-06 | N | DTO mapping is correct (email, role, status)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidAccounts_ShouldMapDtoCorrectly()
        {
            var account = MockAccountRepository.GetActiveUser("USER-001", "alice@tokki.com");
            account.FullName    = "Alice Smith";
            account.Role        = AccountRole.Staff;
            account.PhoneNumber = "0901234567";

            var mockRepo = BuildPagedRepoMock(new List<Account> { account }, 1);
            var query = new GetAllAccountsQuery { PageNumber = 1, PageSize = 10 };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dto = result.Data!.Items.First();
            dto.UserId.Should().Be("USER-001");
            dto.Email.Should().Be("alice@tokki.com");
            dto.FullName.Should().Be("Alice Smith");
            dto.Role.Should().Be(AccountRole.Staff);
            dto.Status.Should().Be(AccountStatus.Active);

            QACollector.LogTestCase("Account - Get All Accounts", new TestCaseDetail
            {
                FunctionGroup   = "Get All Accounts",
                TestCaseID      = "TC-GAA-06",
                Description     = "Account entity correctly mapped to AccountDto",
                ExpectedResult  = "DTO fields (UserId, Email, FullName, Role, Status) match entity values",
                StatusRound1    = "Passed",
                TestCaseType    = "N",
                TestDate        = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "1 account returned by mock",
                    "All key DTO fields verified",
                    "Return 200"
                }
            });
        }
    }
}
