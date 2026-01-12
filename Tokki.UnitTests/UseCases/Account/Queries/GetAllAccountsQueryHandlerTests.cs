using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetAccount;
using Tokki.Domain.Enums;
using Xunit;

// Alias để tránh đụng tên Account ở namespace khác
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.Features.Accounts.Queries
{
    public class GetAllAccountsQueryHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockRepo;
        private readonly GetAllAccountsQueryHandler _handler;

        public GetAllAccountsQueryHandlerTests()
        {
            _mockRepo = new Mock<IAccountRepository>();
            _handler = new GetAllAccountsQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedResult_When_NoFilters()
        {
            // Arrange
            var now = DateTime.UtcNow;

            var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    UserId = "u1",
                    Email = "a@tokki.vn",
                    FullName = "A",
                    PhoneNumber = "0901",
                    Role = AccountRole.User,
                    Status = AccountStatus.Active,
                    CreatedAt = now.AddDays(-2),
                    DateOfBirth = null,
                    VipExpirationDate = null
                },
                new AccountEntity
                {
                    UserId = "u2",
                    Email = "b@tokki.vn",
                    FullName = "B",
                    PhoneNumber = "0902",
                    Role = AccountRole.Staff,
                    Status = AccountStatus.Active,
                    CreatedAt = now.AddDays(-1),
                    DateOfBirth = now.AddYears(-20),
                    VipExpirationDate = now.AddDays(10)
                },
                new AccountEntity
                {
                    UserId = "u3",
                    Email = "c@tokki.vn",
                    FullName = "C",
                    PhoneNumber = "0903",
                    Role = AccountRole.User,
                    Status = AccountStatus.Inactive,
                    CreatedAt = now,
                    DateOfBirth = now.AddYears(-25),
                    VipExpirationDate = now.AddDays(-1)
                }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((accounts, accounts.Count));

            var query = new GetAllAccountsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Lấy danh sách tài khoản thành công");

            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(3);
            result.Data.TotalCount.Should().Be(3);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);

            // Sort desc by CreatedAt => u3, u2, u1
            result.Data.Items[0].UserId.Should().Be("u3");
            result.Data.Items[1].UserId.Should().Be("u2");
            result.Data.Items[2].UserId.Should().Be("u1");

            _mockRepo.Verify(x => x.GetPagedAsync(1, int.MaxValue), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Filter_By_Status_And_Role()
        {
            // Arrange
            var now = DateTime.UtcNow;

            var accounts = new List<AccountEntity>
            {
                new AccountEntity { UserId = "u1", Status = AccountStatus.Active, Role = AccountRole.User, CreatedAt = now.AddDays(-1) },
                new AccountEntity { UserId = "u2", Status = AccountStatus.Active, Role = AccountRole.Staff, CreatedAt = now.AddDays(-2) },
                new AccountEntity { UserId = "u3", Status = AccountStatus.Inactive, Role = AccountRole.User, CreatedAt = now }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((accounts, accounts.Count));

            var query = new GetAllAccountsQuery
            {
                Status = AccountStatus.Active,
                Role = AccountRole.User,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].UserId.Should().Be("u1");
            result.Data.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_Search_By_Name_Email_Phone_CaseInsensitive()
        {
            // Arrange
            var now = DateTime.UtcNow;

            var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    UserId = "u1",
                    FullName = "Nguyen Van A",
                    Email = "AAA@TOKKI.VN",
                    PhoneNumber = "0912345678",
                    CreatedAt = now.AddDays(-1)
                },
                new AccountEntity
                {
                    UserId = "u2",
                    FullName = "Tran B",
                    Email = "bbb@tokki.vn",
                    PhoneNumber = "0999999999",
                    CreatedAt = now
                }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((accounts, accounts.Count));

            var query = new GetAllAccountsQuery
            {
                SearchName = "nguyen",
                SearchEmail = "tokki.vn",
                SearchPhone = "0912",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].UserId.Should().Be("u1");
        }

        [Fact]
        public async Task Handle_Should_Filter_By_VipStatus_Active_Expired_NoVip()
        {
            // Arrange
            var now = DateTime.UtcNow;

            var accounts = new List<AccountEntity>
            {
                new AccountEntity { UserId = "active-vip", VipExpirationDate = now.AddHours(2), CreatedAt = now.AddDays(-1) },
                new AccountEntity { UserId = "expired-vip", VipExpirationDate = now.AddHours(-2), CreatedAt = now.AddDays(-2) },
                new AccountEntity { UserId = "no-vip", VipExpirationDate = null, CreatedAt = now }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((accounts, accounts.Count));

            // 1) Active
            var qActive = new GetAllAccountsQuery { VipStatus = VipStatus.Active, PageNumber = 1, PageSize = 10 };
            var rActive = await _handler.Handle(qActive, CancellationToken.None);
            rActive.IsSuccess.Should().BeTrue();
            rActive.Data.Items.Select(x => x.UserId).Should().ContainSingle().Which.Should().Be("active-vip");

            // 2) Expired
            var qExpired = new GetAllAccountsQuery { VipStatus = VipStatus.Expired, PageNumber = 1, PageSize = 10 };
            var rExpired = await _handler.Handle(qExpired, CancellationToken.None);
            rExpired.IsSuccess.Should().BeTrue();
            rExpired.Data.Items.Select(x => x.UserId).Should().ContainSingle().Which.Should().Be("expired-vip");

            // 3) NoVip
            var qNoVip = new GetAllAccountsQuery { VipStatus = VipStatus.NoVip, PageNumber = 1, PageSize = 10 };
            var rNoVip = await _handler.Handle(qNoVip, CancellationToken.None);
            rNoVip.IsSuccess.Should().BeTrue();
            rNoVip.Data.Items.Select(x => x.UserId).Should().ContainSingle().Which.Should().Be("no-vip");

            _mockRepo.Verify(x => x.GetPagedAsync(1, int.MaxValue), Times.Exactly(3));
        }

        [Fact]
        public async Task Handle_Should_Apply_Pagination_And_Sort_By_CreatedAt_Desc()
        {
            // Arrange
            var now = DateTime.UtcNow;

            var accounts = new List<AccountEntity>
            {
                new AccountEntity { UserId = "u1", CreatedAt = now.AddDays(-3) },
                new AccountEntity { UserId = "u2", CreatedAt = now.AddDays(-2) },
                new AccountEntity { UserId = "u3", CreatedAt = now.AddDays(-1) },
                new AccountEntity { UserId = "u4", CreatedAt = now }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((accounts, accounts.Count));

            var query = new GetAllAccountsQuery
            {
                PageNumber = 2,
                PageSize = 2
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // sort desc => [u4, u3, u2, u1]
            // page 2 size 2 => [u2, u1]
            result.Data.Items.Should().HaveCount(2);
            result.Data.Items[0].UserId.Should().Be("u2");
            result.Data.Items[1].UserId.Should().Be("u1");

            result.Data.TotalCount.Should().Be(4);
            result.Data.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(2);
        }

        [Fact]
        public async Task Handle_Should_Map_To_Dto_And_Default_DateOfBirth_When_Null()
        {
            // Arrange
            var now = DateTime.UtcNow;

            var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    UserId = "u1",
                    Email = "u1@tokki.vn",
                    PhoneNumber = "0901",
                    FullName = "User 1",
                    AvatarUrl = "ava",
                    Role = AccountRole.User,
                    Status = AccountStatus.Active,
                    DateOfBirth = null, // handler map default 2000-01-01
                    CreatedAt = now
                }
            };

            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((accounts, accounts.Count));

            var query = new GetAllAccountsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);

            var dto = result.Data.Items[0];
            dto.UserId.Should().Be("u1");
            dto.Email.Should().Be("u1@tokki.vn");
            dto.PhoneNumber.Should().Be("0901");
            dto.FullName.Should().Be("User 1");
            dto.AvatarUrl.Should().Be("ava");
            dto.Role.Should().Be(AccountRole.User);
            dto.Status.Should().Be(AccountStatus.Active);

            dto.DateOfBirth.Should().Be(new DateTime(2000, 1, 1));
        }

        [Fact]
        public async Task Handle_Should_ReturnEmpty_When_NoData()
        {
            // Arrange
            _mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((new List<AccountEntity>(), 0));

            var query = new GetAllAccountsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeNull();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }
    }
}
