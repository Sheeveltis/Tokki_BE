using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.ResetPassword;
using BCrypt.Net;
using Xunit;

// Alias để tránh trùng namespace
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class ResetPasswordCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IValidator<ResetPasswordCommand>> _mockValidator;

        private readonly ResetPasswordCommandHandler _handler;

        public ResetPasswordCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockValidator = new Mock<IValidator<ResetPasswordCommand>>();

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _handler = new ResetPasswordCommandHandler(
                _mockAccountRepo.Object,
                _mockValidator.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldResetPassword_WhenEmailExists()
        {
            // Arrange
            var email = "user@example.com";
            var newPassword = "NewPassword123";

            var user = new AccountEntity
            {
                UserId = "U1",
                Email = email,
                PasswordHash = "OLD_HASH"
            };

            _mockAccountRepo
                .Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);

            AccountEntity? updatedUser = null;

            _mockAccountRepo
                .Setup(r => r.UpdateUserAsync(It.IsAny<AccountEntity>()))
                .Callback<AccountEntity>(u => updatedUser = u)
                .Returns(Task.CompletedTask);

            var command = new ResetPasswordCommand
            {
                Email = email,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(updatedUser);
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedUser.PasswordHash));
        }

        [Fact]
        public async Task Handle_ShouldReturnError_WhenEmailNotFound()
        {
            var command = new ResetPasswordCommand
            {
                Email = "none@example.com",
                NewPassword = "123456",
                ConfirmPassword = "123456"
            };

            _mockAccountRepo
                .Setup(r => r.GetByEmailAsync(command.Email))
                .ReturnsAsync((AccountEntity)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.Code == "User.NotFound");

            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
        }
    }
}
