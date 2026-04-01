using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.DeleteAccount;
using Tokki.Domain.Enums;
using Xunit;

// Alias tránh lỗi trùng/nhầm namespace Account
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class DeleteAccountCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly DeleteAccountCommandHandler _handler;

        public DeleteAccountCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _handler = new DeleteAccountCommandHandler(_mockAccountRepo.Object);
        }

        // ==========================================
        // CASE 1: LỖI - UserId null/empty (Unauthorized)
        // ==========================================
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserId_IsNullOrEmpty()
        {
            // Arrange
            var command = new DeleteAccountCommand
            {
                UserId = null
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserUnauthorized.Code);

            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ==========================================
        // CASE 2: LỖI - User không tồn tại
        // ==========================================
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var command = new DeleteAccountCommand
            {
                UserId = "user-not-found"
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.UserId))
                            .ReturnsAsync((AccountEntity?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserNotFoundById.Code);

            _mockAccountRepo.Verify(x => x.GetByIdAsync(command.UserId), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ==========================================
        // CASE 3: LỖI - Tài khoản đã bị xóa trước đó (Inactive)
        // ==========================================
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountAlreadyDeleted()
        {
            // Arrange
            var command = new DeleteAccountCommand
            {
                UserId = "user-01"
            };

            var inactiveUser = new AccountEntity
            {
                UserId = command.UserId,
                Status = AccountStatus.Inactive,
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.UserId))
                            .ReturnsAsync(inactiveUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == "Account.AlreadyDeleted");
            result.Errors.Should().Contain(e => e.Description == "The account was previously deleted.");

            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UserIsActive()
        {
            // Arrange
            var command = new DeleteAccountCommand
            {
                UserId = "user-active"
            };

            var activeUser = new AccountEntity
            {
                UserId = command.UserId,
                Status = AccountStatus.Active,
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-2)
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.UserId))
                            .ReturnsAsync(activeUser);

            AccountEntity? updatedEntity = null;
            _mockAccountRepo.Setup(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()))
                            .Callback<AccountEntity>(u => updatedEntity = u)
                            .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // IMPORTANT: Với Success("...", 200) thì chuỗi thường nằm ở Data, không phải Message
            result.Data.Should().Be("Account has been successfully deleted!");

            _mockAccountRepo.Verify(x => x.GetByIdAsync(command.UserId), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            updatedEntity.Should().NotBeNull();
            updatedEntity!.Status.Should().Be(AccountStatus.Inactive);

            // Nếu UpdatedAt là DateTime (không nullable) dùng thẳng
            updatedEntity.UpdatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));
        }

    }
}
