using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetInternalUserVipAccounts;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class GetInternalUserVipAccountsQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetInternalUserVipAccountsQueryHandler CreateHandler(Mock<IAccountRepository>? repo = null)
        {
            return new GetInternalUserVipAccountsQueryHandler((repo ?? MockAccountRepository.GetMock()).Object);
        }

        private static List<Account> GetSampleAccounts()
        {
            var u1 = MockAccountRepository.GetActiveUser("U1", "alice@test.com");
            u1.FullName = "Alice User";
            u1.PhoneNumber = "0111111111";
            u1.Role = AccountRole.User;
            u1.Status = AccountStatus.Active;

            var u2 = MockAccountRepository.GetActiveUser("U2", "bob@test.com");
            u2.FullName = "Bob Vip";
            u2.PhoneNumber = "0222222222";
            u2.Role = AccountRole.Vip;
            u2.Status = AccountStatus.Inactive;
            u2.VipExpirationDate = DateTime.UtcNow.AddDays(10); // Active VIP

            var u3 = MockAccountRepository.GetActiveUser("U3", "charlie@test.com");
            u3.FullName = "Charlie Staff";
            u3.PhoneNumber = "0333333333";
            u3.Role = AccountRole.Staff; // Should be excluded

            var u4 = MockAccountRepository.GetActiveUser("U4", "david@test.com");
            u4.FullName = "David Expired";
            u4.PhoneNumber = "0444444444";
            u4.Role = AccountRole.Vip;
            u4.VipExpirationDate = DateTime.UtcNow.AddDays(-5); // Expired VIP

            return new List<Account> { u1, u2, u3, u4 };
        }

        private static Mock<IAccountRepository> BuildMockRepo(List<Account> accounts)
        {
            var m = new Mock<IAccountRepository>();
            m.Setup(x => x.GetPagedAsync(1, int.MaxValue))
             .ReturnsAsync((accounts, accounts.Count));
            return m;
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Internal_User_Vip_Accounts_01 | N | Base Filter: Only User or VIP roles are returned
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoFilters_ShouldReturnOnlyUserAndVipRoles()
        {
            var repo = BuildMockRepo(GetSampleAccounts());
            var query = new GetInternalUserVipAccountsQuery { PageNumber = 1, PageSize = 10 };

            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(3); // Alice, Bob, David (Charlie is Staff)
            result.Data.Items.Should().NotContain(a => a.Role == AccountRole.Staff);

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup     = "Get Internal User Vip Accounts",
                TestCaseID        = "Get_Internal_User_Vip_Accounts_01",
                Description       = "Query without specific filters should exclude Admin/Staff roles",
                ExpectedResult    = "Return only AccountRole.User and AccountRole.Vip",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Base filter applied", "Return 3 valid accounts" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Internal_User_Vip_Accounts_02 | N | Filter by Status works correctly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusFilter_ShouldReturnMatchingStatus()
        {
            var repo = BuildMockRepo(GetSampleAccounts());
            var query = new GetInternalUserVipAccountsQuery { Status = AccountStatus.Inactive, PageNumber = 1, PageSize = 10 };

            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1); // Only Bob is Inactive
            result.Data.Items.First().Role.Should().Be(AccountRole.Vip);

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup     = "Get Internal User Vip Accounts",
                TestCaseID        = "Get_Internal_User_Vip_Accounts_02",
                Description       = "Filter by Status = Inactive",
                ExpectedResult    = "Return exactly 1 matching account",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Inactive", "Return 1 account" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Internal_User_Vip_Accounts_03 | N | Search by partial Name
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SearchNameFilter_ShouldReturnMatchingName()
        {
            var repo = BuildMockRepo(GetSampleAccounts());
            var query = new GetInternalUserVipAccountsQuery { SearchName = "ali", PageNumber = 1, PageSize = 10 };

            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().FullName.Should().Be("Alice User");

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup     = "Get Internal User Vip Accounts",
                TestCaseID        = "Get_Internal_User_Vip_Accounts_03",
                Description       = "Case-insensitive SearchName filter",
                ExpectedResult    = "Return Alice's account",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchName = 'ali'", "Return 1 account" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Internal_User_Vip_Accounts_04 | N | VIP Status Active vs NoVip
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VipStatusActiveFilter_ShouldReturnOnlyFutureVips()
        {
            var repo = BuildMockRepo(GetSampleAccounts());
            var query = new GetInternalUserVipAccountsQuery { VipStatus = VipStatus.Active, PageNumber = 1, PageSize = 10 };

            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().FullName.Should().Be("Bob Vip"); // David's VIP is expired

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup     = "Get Internal User Vip Accounts",
                TestCaseID        = "Get_Internal_User_Vip_Accounts_04",
                Description       = "Filter by VipStatus = Active",
                ExpectedResult    = "Return accounts where VipExpirationDate > now",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipStatus = Active", "Return 1 account" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Internal_User_Vip_Accounts_05 | N | VIP Status NoVIP
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VipStatusNoVipFilter_ShouldReturnOnlyNullVips()
        {
            var repo = BuildMockRepo(GetSampleAccounts());
            var query = new GetInternalUserVipAccountsQuery { VipStatus = VipStatus.NoVip, PageNumber = 1, PageSize = 10 };

            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().FullName.Should().Be("Alice User"); // Alice has no VIP setup

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup     = "Get Internal User Vip Accounts",
                TestCaseID        = "Get_Internal_User_Vip_Accounts_05",
                Description       = "Filter by VipStatus = NoVip",
                ExpectedResult    = "Return accounts where VipExpirationDate == null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipStatus = NoVip", "Return 1 account" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Internal_User_Vip_Accounts_06 | B | Paging skips correctly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Paging_ShouldHonorPageNumberAndSize()
        {
            var repo = BuildMockRepo(GetSampleAccounts());
            // Total matched is 3. Let's take page 2, size 2 (should return 1)
            var query = new GetInternalUserVipAccountsQuery { PageNumber = 2, PageSize = 2 };

            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.TotalCount.Should().Be(3);
            result.Data.Items.Should().HaveCount(1);
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("Account - Get VIP Accounts", new TestCaseDetail
            {
                FunctionGroup     = "Get Internal User Vip Accounts",
                TestCaseID        = "Get_Internal_User_Vip_Accounts_06",
                Description       = "Paging with pageSize=2, pageNumber=2 on 3 items",
                ExpectedResult    = "Return 1 item on the 2nd page",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PageNumber = 2", "PageSize = 2", "Return exact 1 item" }
            });
        }
    }
}
