using FluentAssertions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;
using EntityAccount = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts
{
    public class LoginCommandHandlerTests : AccountTestBase
    {
        // ==========================================
        // CASE 1: ĐĂNG NHẬP THÀNH CÔNG
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenCredentialsAreCorrect()
        {
            // Arrange
            var email = "success@tokki.vn";
            var password = "Password123!";

            var user = AccountTestData.GetValidAccount(email, password);
            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<EntityAccount>(), It.IsAny<DateTime>()))
                .Returns("fake-jwt-token");

            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>()))
                .Returns("session-id-123");

            // Không bắt buộc setup TOKEN_EXPIRATION_MINUTES, handler default 60 nếu null
            // Không bắt buộc setup DEFAULT_PASSWORD_*; Moq mặc định trả null => không bị chặn

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Đăng nhập thành công!");
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be("fake-jwt-token");

            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockGamificationService.Verify(
                x => x.CheckLoginGamificationAsync(It.Is<Tokki.Domain.Entities.Account>(a => a.UserId == user.UserId)),
                Times.Once
            );
            _mockEmailHistoryRepository.Verify(x =>
                x.DeleteByUserAndTemplateTypeAsync(
                    user.UserId,
                    EmailTemplateType.OfflineReminder,
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );

            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        // ==========================================
        // CASE 2: SAI EMAIL (USER KHÔNG TỒN TẠI)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
        {
            // Arrange
            var email = "notfound@tokki.vn";
            var command = AccountTestData.GetLoginCommand(email, "any-password");

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((EntityAccount)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Tài khoản không tồn tại.");

            result.Errors.Should().NotBeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Code.Should().Be(AppErrors.UserNotFound.Code);
            result.Errors.First().Description.Should().Be(AppErrors.UserNotFound.Description);

            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Never);
        }

        // ==========================================
        // CASE 3: SAI MẬT KHẨU
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPasswordIsWrong()
        {
            // Arrange
            var email = "wrongpass@tokki.vn";
            var correctPassword = "CorrectPassword";
            var wrongInputPassword = "WrongInput";

            var user = AccountTestData.GetValidAccount(email, correctPassword);
            var command = AccountTestData.GetLoginCommand(email, wrongInputPassword);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("LOGIN_FAILED_LIMIT"))
                .ReturnsAsync("5");
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("LOGIN_LOCKOUT_DURATION_LEVEL_1"))
                .ReturnsAsync("300");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be(AppErrors.WrongPassword.Description);

            result.Errors.Should().NotBeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Code.Should().Be(AppErrors.WrongPassword.Code);
            result.Errors.First().Description.Should().Be(AppErrors.WrongPassword.Description);

            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.Is<EntityAccount>(u => u.FailedLoginCount == 1)), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Never);
        }

        // ==========================================
        // CASE 4: TÀI KHOẢN BỊ KHÓA VĨNH VIỄN (BANNED)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsBanned()
        {
            // Arrange
            var email = "banned@tokki.vn";
            var password = "password123";

            var user = AccountTestData.GetBannedAccount(email, password);
            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Be("Tài khoản của bạn đã bị khóa vĩnh viễn.");

            result.Errors.Should().NotBeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Code.Should().Be(AppErrors.AccountBanned.Code);
            result.Errors.First().Description.Should().Be(AppErrors.AccountBanned.Description);

            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Never);
        }

        // ==========================================
        // CASE 5: TÀI KHOẢN BỊ TẠM KHÓA (LOCKED)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsLocked()
        {
            // Arrange
            var email = "locked@tokki.vn";
            var password = "password123";

            var user = AccountTestData.GetValidAccount(email, password);

            // Handler so sánh với vietnamTimeNow = UtcNow + 7
            // nên set LockedUntil dựa theo UtcNow+7 để chắc chắn > vietnamTimeNow
            user.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(10);

            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Contain("Tài khoản đang bị tạm khóa. Thử lại sau");

            result.Errors.Should().NotBeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Code.Should().Be(AppErrors.AccountLocked.Code);
            result.Errors.First().Description.Should().Be(AppErrors.AccountLocked.Description);

            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Never);
        }

        // ==========================================
        // CASE 6: RESET FAILEDLOGINCOUNT KHI ĐĂNG NHẬP THÀNH CÔNG
        // ==========================================
        [Fact]
        public async Task Handle_ShouldResetFailedLoginCount_WhenLoginSuccessAfterFailures()
        {
            // Arrange
            var email = "reset@tokki.vn";
            var password = "Password123!";

            var user = AccountTestData.GetValidAccount(email, password);
            user.FailedLoginCount = 3;
            user.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(-1); // đã hết lock

            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<EntityAccount>(), It.IsAny<DateTime>()))
                .Returns("fake-jwt-token");

            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>()))
                .Returns("session-id-123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            _mockAccountRepo.Verify(x =>
                x.UpdateUserAsync(It.Is<EntityAccount>(u => u.FailedLoginCount == 0 && u.LockedUntil == null)),
                Times.Once
            );
        }

        // ==========================================
        // CASE 7: TÀI KHOẢN INACTIVE
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsInactive()
        {
            // Arrange
            var email = "inactive@tokki.vn";
            var password = "Password123!";

            var user = AccountTestData.GetValidAccount(email, password);
            user.Status = AccountStatus.Inactive;

            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Be("Tài khoản của bạn không hoạt động.");

            result.Errors.Should().NotBeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Code.Should().Be(AppErrors.AccountInActive.Code);
            result.Errors.First().Description.Should().Be(AppErrors.AccountInActive.Description);

            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Never);
        }

        // ==========================================
        // CASE 8: CHẶN MẬT KHẨU MẶC ĐỊNH (DEFAULT PASSWORD)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUsingDefaultPassword()
        {
            // Arrange
            var email = "defaultpass@tokki.vn";
            var defaultPassword = "Tokki@Default123"; // giả lập mật khẩu mặc định từ config

            var user = AccountTestData.GetValidAccount(email, defaultPassword);
            var command = AccountTestData.GetLoginCommand(email, defaultPassword);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Đúng mật khẩu nhưng bị chặn do trùng default password
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER"))
                .ReturnsAsync(defaultPassword);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Be(AppErrors.DefaultPasswordUsed.Description);

            result.Errors.Should().NotBeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Code.Should().Be(AppErrors.DefaultPasswordUsed.Code);
            result.Errors.First().Description.Should().Be(AppErrors.DefaultPasswordUsed.Description);

            // Vì bị chặn trước đoạn tạo session/token
            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
