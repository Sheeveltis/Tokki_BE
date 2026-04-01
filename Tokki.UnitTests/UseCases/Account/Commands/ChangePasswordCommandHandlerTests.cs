using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.ChangePassword;
using Tokki.Domain.Enums;
using Xunit;

// Alias để tránh lỗi "Account is a namespace but is used like a type"
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class ChangePasswordCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly ChangePasswordCommandHandler _handler;

        public ChangePasswordCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _handler = new ChangePasswordCommandHandler(_mockAccountRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                Email = "notfound@tokki.vn",
                OldPassword = "Old123!",
                NewPassword = "New123!",
                ConfirmNewPassword = "New123!"
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync((AccountEntity?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserNotFound.Code);

            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_OldPasswordIsIncorrect()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                Email = "user@tokki.vn",
                OldPassword = "WrongOldPassword",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };

            var user = new AccountEntity
            {
                UserId = "U1",
                Email = command.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealOldPassword"), // khác OldPassword
                FailedLoginCount = 3,
                LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(10),
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.InvalidCredentials.Code);

            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_RequestIsValid()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                Email = "user@tokki.vn",
                OldPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };

            var user = new AccountEntity
            {
                UserId = "U1",
                Email = command.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.OldPassword), // đúng old
                FailedLoginCount = 5,
                LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(10),
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(user);

            AccountEntity? updatedEntity = null;
            _mockAccountRepo.Setup(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()))
                            .Callback<AccountEntity>(u => updatedEntity = u)
                            .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Handler đang gọi: Success("Password changed successfully!", 200)
            // => string này thường nằm ở Data (không phải Message)
            result.Data.Should().Be("Password changed successfully!");

            _mockAccountRepo.Verify(x => x.GetByEmailAsync(command.Email), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            updatedEntity.Should().NotBeNull();

            // Verify hash đúng (đặt ngoài Moq expression để tránh lỗi optional args)
            BCrypt.Net.BCrypt.Verify(command.NewPassword, updatedEntity!.PasswordHash).Should().BeTrue();

            // Verify reset lock state
            updatedEntity.FailedLoginCount.Should().Be(0);
            updatedEntity.LockedUntil.Should().BeNull();
        }
    }
}
