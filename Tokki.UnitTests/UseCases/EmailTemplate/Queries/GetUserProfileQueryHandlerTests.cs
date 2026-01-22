using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetUserProfile;
using Tokki.Domain.Enums;
using Xunit;

// Alias để tránh nhầm Account (namespace) với type
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.Features.Accounts.Queries
{
    public class GetUserProfileQueryHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockRepo;
        private readonly GetUserProfileQueryHandler _handler;

        public GetUserProfileQueryHandlerTests()
        {
            _mockRepo = new Mock<IAccountRepository>();
            _handler = new GetUserProfileQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var query = new GetUserProfileQuery("not-exist");

            _mockRepo.Setup(x => x.GetByIdAsync(query.UserId))
                     .ReturnsAsync((AccountEntity?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Không tìm thấy người dùng.");

            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == "User.NotFound");

            _mockRepo.Verify(x => x.GetByIdAsync(query.UserId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_MapFields_When_UserFound_WithDateOfBirth()
        {
            // Arrange
            var userId = "u-01";
            var dob = new DateTime(2000, 1, 2);

            var user = new AccountEntity
            {
                UserId = userId,
                Email = "user@tokki.vn",
                FullName = "User Name",
                PhoneNumber = "0912345678",
                AvatarUrl = "https://img",
                DateOfBirth = dob,

                Role = AccountRole.User,
                Status = AccountStatus.Active,

                TotalXP = 1000,
                AchievedGoalStreak = 5,
                MaxStreak = 10,

                CurrentTitleId = "title-01",

                // Level là TopicLevel? => phải gán enum (và enum chỉ có 1..6)
                Level = Tokki.Domain.Enums.TopicLevel.Level3,

                LastLoginAt = DateTime.UtcNow.AddHours(7).AddMinutes(-5)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(userId))
                     .ReturnsAsync(user);

            var query = new GetUserProfileQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();

            var dto = result.Data!;

            dto.UserId.Should().Be(user.UserId);
            dto.Email.Should().Be(user.Email);
            dto.FullName.Should().Be(user.FullName);
            dto.PhoneNumber.Should().Be(user.PhoneNumber);
            dto.AvatarUrl.Should().Be(user.AvatarUrl);

            dto.DateOfBirth.Should().Be(DateOnly.FromDateTime(dob));

            dto.Role.Should().Be(user.Role);
            dto.Status.Should().Be(user.Status);

            dto.TotalXP.Should().Be(user.TotalXP);
            dto.AchievedGoalStreak.Should().Be(user.AchievedGoalStreak);
            dto.MaxStreak.Should().Be(user.MaxStreak);

            dto.CurrentTitle.Should().Be(user.CurrentTitleId);

            // Level là TopicLevel? => so sánh enum
            dto.Level.Should().Be(user.Level);

            dto.LastLoginAt.Should().Be(user.LastLoginAt);

            _mockRepo.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_DateOfBirthNull_When_UserDateOfBirthIsNull()
        {
            // Arrange
            var userId = "u-02";

            var user = new AccountEntity
            {
                UserId = userId,
                Email = "user2@tokki.vn",
                FullName = "User 2",
                DateOfBirth = null,
                Role = AccountRole.User,
                Status = AccountStatus.Active
            };

            _mockRepo.Setup(x => x.GetByIdAsync(userId))
                     .ReturnsAsync(user);

            var query = new GetUserProfileQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            result.Data.Should().NotBeNull();
            result.Data!.DateOfBirth.Should().BeNull();
        }
    }
}
