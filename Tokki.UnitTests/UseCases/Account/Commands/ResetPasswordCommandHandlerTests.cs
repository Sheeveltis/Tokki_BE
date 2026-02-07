using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.ResetPassword;
using Xunit;

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

            // Handler hiện tại không gọi validator, nhưng giữ setup để tránh side effects nếu sau này thêm validate
            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockAccountRepo
                .Setup(r => r.UpdateUserAsync(It.IsAny<AccountEntity>()))
                .Returns(Task.CompletedTask);

            _mockAccountRepo
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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
                PasswordHash = "OLD_HASH",
                FailedLoginCount = 5,
                LockedUntil = System.DateTime.UtcNow.AddHours(7).AddMinutes(10)
            };

            _mockAccountRepo
                .Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);

            AccountEntity? updatedUser = null;
            _mockAccountRepo
                .Setup(r => r.UpdateUserAsync(It.IsAny<AccountEntity>()))
                .Callback<AccountEntity>(u => updatedUser = u)
                .Returns(Task.CompletedTask);

            _mockAccountRepo
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
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
            Assert.Equal(200, result.StatusCode);

            // DÒNG ĐÚNG: handler đang trả success với DATA là "Đổi mật khẩu thành công!"
            Assert.Equal("Đổi mật khẩu thành công!", result.Data);

            // Message có thể null/empty tuỳ implementation OperationResult
            // Nếu bạn muốn kiểm tra thì chỉ nên check không bắt buộc:
            // Assert.True(string.IsNullOrEmpty(result.Message));

            _mockAccountRepo.Verify(r => r.GetByEmailAsync(email), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(updatedUser);

            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedUser!.PasswordHash));
            Assert.Equal(0, updatedUser.FailedLoginCount);
            Assert.Null(updatedUser.LockedUntil);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenEmailNotFound()
        {
            // Arrange
            var command = new ResetPasswordCommand
            {
                Email = "none@example.com",
                NewPassword = "123456",
                ConfirmPassword = "123456"
            };

            _mockAccountRepo
                .Setup(r => r.GetByEmailAsync(command.Email))
                .ReturnsAsync((AccountEntity)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);

            // Không hardcode "User.NotFound" vì Code thực tế phụ thuộc AppErrors.UserNotFound.Code
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Code == AppErrors.UserNotFound.Code);

            _mockAccountRepo.Verify(r => r.GetByEmailAsync(command.Email), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
