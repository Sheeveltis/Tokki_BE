using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.AdminSoftDeleteAccount;
using Tokki.Domain.Enums;
using Xunit;

// Alias để tránh lỗi: 'Account' is a namespace but is used like a type
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class AdminSoftDeleteAccountCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly AdminSoftDeleteAccountCommandHandler _handler;

        public AdminSoftDeleteAccountCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _handler = new AdminSoftDeleteAccountCommandHandler(_mockAccountRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AdminUserId_IsNullOrEmpty()
        {
            // Arrange
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = null,
                TargetUserId = "user-01"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserUnauthorized.Code);

            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TargetUserId_IsNullOrEmpty()
        {
            // Arrange
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "admin-01",
                TargetUserId = "   "
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.TargetUserIdRequired.Code);

            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AdminTriesToDisableSelf()
        {
            // Arrange
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "ADMIN-01",
                TargetUserId = "admin-01"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.CannotDisableSelf.Code);

            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TargetUser_NotFound()
        {
            // Arrange
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "admin-01",
                TargetUserId = "user-not-found"
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync((AccountEntity?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserNotFoundById.Code);

            _mockAccountRepo.Verify(x => x.GetByIdAsync(command.TargetUserId), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TargetUser_AlreadyInactive()
        {
            // Arrange
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "admin-01",
                TargetUserId = "user-01"
            };

            var inactiveUser = new AccountEntity
            {
                UserId = command.TargetUserId,
                Status = AccountStatus.Inactive
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync(inactiveUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountAlreadyInactive.Code);

            _mockAccountRepo.Verify(x => x.GetByIdAsync(command.TargetUserId), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TargetUser_IsActive()
        {
            // Arrange
            var command = new AdminSoftDeleteAccountCommand
            {
                AdminUserId = "admin-01",
                TargetUserId = "user-01"
            };

            var activeUser = new AccountEntity
            {
                UserId = command.TargetUserId,
                Status = AccountStatus.Active,
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync(activeUser);

            AccountEntity? updatedEntity = null;
            _mockAccountRepo.Setup(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()))
                            .Callback<AccountEntity>(u => updatedEntity = u)
                            .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Chuỗi success của handler đang nằm ở Data, không phải Message
            result.Data.Should().Be("Successfully deactivated the user's account.");

            _mockAccountRepo.Verify(x => x.GetByIdAsync(command.TargetUserId), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            updatedEntity.Should().NotBeNull();
            updatedEntity!.Status.Should().Be(AccountStatus.Inactive);
            updatedEntity.UpdatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));
        }

    }
}
