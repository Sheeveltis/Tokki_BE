using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Accounts.Queries.GetAccountDetailById;
using Tokki.Domain.Enums;
using Xunit;

// Alias để tránh nhầm Account (namespace) với type
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.Features.Accounts.Queries
{
    public class GetAccountDetailByIdQueryHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockRepo;
        private readonly GetAccountDetailByIdQueryHandler _handler;

        public GetAccountDetailByIdQueryHandlerTests()
        {
            _mockRepo = new Mock<IAccountRepository>();
            _handler = new GetAccountDetailByIdQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountNotFound()
        {
            // Arrange
            var query = new GetAccountDetailByIdQuery
            {
                UserId = "not-exist"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(query.UserId))
                     .ReturnsAsync((AccountEntity?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountNotFound.Code);

            _mockRepo.Verify(x => x.GetByIdAsync(query.UserId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_MapAllFields_When_Found()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7);

            var query = new GetAccountDetailByIdQuery
            {
                UserId = "u-01"
            };

            var account = new AccountEntity
            {
                UserId = "u-01",
                Email = "user@tokki.vn",
                PhoneNumber = "0912345678",
                DateOfBirth = now.AddYears(-20),
                PasswordHash = "HASH",
                FullName = "User Name",
                AvatarUrl = "https://img",
                Role = AccountRole.User,
                Status = AccountStatus.Active,
                VipExpirationDate = now.AddDays(30),
                TotalXP = 1000,
                AchievedGoalStreak = 5,
                MaxStreak = 10,
                LastStreakDate = now.Date,
                DailyStudySeconds = 1200,
                CurrentTitleId = "title-01",
                FailedLoginCount = 2,
                LockedUntil = now.AddMinutes(10),
                LastLoginAt = now.AddHours(-1),
                CreatedAt = now.AddDays(-100),
                UpdatedAt = now.AddDays(-1)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(query.UserId))
                     .ReturnsAsync(account);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Retrieve account details successfully");

            result.Data.Should().NotBeNull();
            var dto = result.Data!;

            dto.UserId.Should().Be(account.UserId);
            dto.Email.Should().Be(account.Email);
            dto.PhoneNumber.Should().Be(account.PhoneNumber);
            dto.DateOfBirth.Should().Be(account.DateOfBirth!.Value);
            dto.PasswordHash.Should().Be(account.PasswordHash);
            dto.FullName.Should().Be(account.FullName);
            dto.AvatarUrl.Should().Be(account.AvatarUrl);
            dto.Role.Should().Be(account.Role);
            dto.Status.Should().Be(account.Status);
            dto.VipExpirationDate.Should().Be(account.VipExpirationDate);
            dto.TotalXP.Should().Be(account.TotalXP);
            dto.AchievedGoalStreak.Should().Be(account.AchievedGoalStreak);
            dto.MaxStreak.Should().Be(account.MaxStreak);
            dto.LastStreakDate.Should().Be(account.LastStreakDate);
            dto.DailyStudySeconds.Should().Be(account.DailyStudySeconds);
            dto.CurrentTitleId.Should().Be(account.CurrentTitleId);
            dto.FailedLoginCount.Should().Be(account.FailedLoginCount);
            dto.LockedUntil.Should().Be(account.LockedUntil);
            dto.LastLoginAt.Should().Be(account.LastLoginAt);
            dto.CreatedAt.Should().Be(account.CreatedAt);
            dto.UpdatedAt.Should().Be(account.UpdatedAt);

            _mockRepo.Verify(x => x.GetByIdAsync(query.UserId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Default_DateOfBirth_To_2000_01_01_When_Null()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7);

            var query = new GetAccountDetailByIdQuery
            {
                UserId = "u-02"
            };

            var account = new AccountEntity
            {
                UserId = "u-02",
                Email = "user2@tokki.vn",
                DateOfBirth = null, // default in handler
                PasswordHash = "HASH2",
                FullName = "User 2",
                Role = AccountRole.User,
                Status = AccountStatus.Active,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-1)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(query.UserId))
                     .ReturnsAsync(account);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.DateOfBirth.Should().Be(new DateTime(2000, 1, 1));
        }
    }
}
