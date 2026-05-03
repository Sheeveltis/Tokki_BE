using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetInternalUserVipAccounts;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Queries
{
    public class GetInternalUserVipAccountsQueryHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();

        private GetInternalUserVipAccountsQueryHandler CreateHandler()
        {
            return new GetInternalUserVipAccountsQueryHandler(_accountRepoMock.Object);
        }

        private List<Account> GetSampleAccounts()
        {
            return new List<Account>
            {
                new Account { UserId = "1", Email = "u1@test.com", Role = AccountRole.User, Status = AccountStatus.Active, FullName = "User One", PhoneNumber = "111", CreatedAt = DateTime.UtcNow },
                new Account { UserId = "2", Email = "u2@test.com", Role = AccountRole.Admin, Status = AccountStatus.Active, FullName = "Admin Two", PhoneNumber = "222", CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
                new Account { UserId = "3", Email = "u3@test.com", Role = AccountRole.Vip, Status = AccountStatus.Banned, FullName = "Vip Three", PhoneNumber = "333", VipExpirationDate = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
                new Account { UserId = "4", Email = null, Role = AccountRole.Vip, Status = AccountStatus.Active, FullName = null, PhoneNumber = null, VipExpirationDate = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow.AddMinutes(-3) }
            };
        }

        // ═══════════════════════════════════════════════════════════
        // GetInternalUserVipAccountsQueryHandler_01 | N | Returns Only User and Vip Roles
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ShouldFilterOnlyUserAndVipRoles()
        {
            var command = new GetInternalUserVipAccountsQuery { PageNumber = 1, PageSize = 10 };
            _accountRepoMock.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((GetSampleAccounts(), 4));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(3); 
            result.Data.Items.Should().NotContain(x => x.Role == AccountRole.Admin);

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup = "GetInternalUserVipAccountsQueryHandler",
                TestCaseID = "GetInternalUserVipAccountsQueryHandler_01",
                Description = "Filters out roles other than User and Vip",
                ExpectedResult = "Return 3 accounts",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No other filters" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetInternalUserVipAccountsQueryHandler_02 | N | Status Filter
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusFilter_ShouldApply()
        {
            var command = new GetInternalUserVipAccountsQuery { PageNumber = 1, PageSize = 10, Status = AccountStatus.Banned };
            _accountRepoMock.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((GetSampleAccounts(), 4));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].UserId.Should().Be("3");

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup = "GetInternalUserVipAccountsQueryHandler",
                TestCaseID = "GetInternalUserVipAccountsQueryHandler_02",
                Description = "Filters correctly by status",
                ExpectedResult = "Return 1 account with banned status",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Banned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetInternalUserVipAccountsQueryHandler_03 | N | Name, Email, Phone Filters
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TextFilters_ShouldApply()
        {
            var command = new GetInternalUserVipAccountsQuery { SearchName = "user", SearchEmail = "u1", SearchPhone = "111", PageNumber = 1, PageSize = 10 };
            _accountRepoMock.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((GetSampleAccounts(), 4));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].UserId.Should().Be("1");

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup = "GetInternalUserVipAccountsQueryHandler",
                TestCaseID = "GetInternalUserVipAccountsQueryHandler_03",
                Description = "Filters correctly by text inputs",
                ExpectedResult = "Return 1 matching account",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchName, SearchEmail, SearchPhone provided safely" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetInternalUserVipAccountsQueryHandler_04 | N | Vip Status Filter Active
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VipStatusActive_ShouldReturnOnlyActiveVip()
        {
            var command = new GetInternalUserVipAccountsQuery { PageNumber = 1, PageSize = 10, VipStatus = VipStatus.Active };
            _accountRepoMock.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((GetSampleAccounts(), 4));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].UserId.Should().Be("3");

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup = "GetInternalUserVipAccountsQueryHandler",
                TestCaseID = "GetInternalUserVipAccountsQueryHandler_04",
                Description = "Filters active VIP users",
                ExpectedResult = "Return users with future VIP dates",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipStatus = Active" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetInternalUserVipAccountsQueryHandler_05 | N | Vip Status NoVip
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VipStatusNoVip_ShouldReturnUsersWithNoDate()
        {
            var command = new GetInternalUserVipAccountsQuery { PageNumber = 1, PageSize = 10, VipStatus = VipStatus.NoVip };
            _accountRepoMock.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((GetSampleAccounts(), 4));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].UserId.Should().Be("1");

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup = "GetInternalUserVipAccountsQueryHandler",
                TestCaseID = "GetInternalUserVipAccountsQueryHandler_05",
                Description = "Filters Non-VIP users",
                ExpectedResult = "Return users with null VIP dates",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipStatus = NoVip" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetInternalUserVipAccountsQueryHandler_06 | B | Empty Data Case
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyData_ShouldReturnEmptyPagedResult()
        {
            var command = new GetInternalUserVipAccountsQuery { PageNumber = 2, PageSize = 10 };
            _accountRepoMock.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((new List<Account>(), 0));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup = "GetInternalUserVipAccountsQueryHandler",
                TestCaseID = "GetInternalUserVipAccountsQueryHandler_06",
                Description = "Returns empty list safely if repository is empty",
                ExpectedResult = "Return 0 length items",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo items = 0" }
            });
        }
    }
}
