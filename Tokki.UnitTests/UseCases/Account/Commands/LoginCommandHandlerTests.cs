using Moq;
using Xunit;
using FluentValidation;
using FluentValidation.Results;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using System.Threading;
using System.Threading.Tasks;
using EntityAccount = Tokki.Domain.Entities.Account;
using Tokki.Application.UseCases.Accounts.Queries.Login;
using System.Linq;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.UseCases.Accounts
{
    public class LoginCommandHandlerTests : AccountTestBase
    {
        // Mock riêng biệt của Use Case này 
        private readonly Mock<IValidator<LoginCommand>> _mockValidator;
        private readonly LoginCommandHandler _handler;
        private readonly Mock<IGamificationService> _mockGamificationService;
        public LoginCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<LoginCommand>>();

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _handler = new LoginCommandHandler(
                _mockAccountRepo.Object,
                _mockSystemConfigRepo.Object,
                _mockJwtGenerator.Object,
                _mockIdGenerator.Object,
                _mockGamificationService.Object, 
                _mockValidator.Object
            );
        }

        // ==========================================
        // CASE 1: ĐĂNG NHẬP THÀNH CÔNG
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenCredentialsAreCorrect()
        {
            // Arrange
            var email = "success@tokki.vn";
            var password = "Password123!";

            var mockUser = AccountTestData.GetValidAccount(email, password);
            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(mockUser);

            _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<EntityAccount>(), It.IsAny<DateTime>()))
                .Returns("fake-jwt-token");

            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>()))
                .Returns("session-id-123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Đăng nhập thành công!", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("fake-jwt-token", result.Data.Token);

            // Verify
            _mockAccountRepo.Verify(x => x.AddSessionAsync(It.IsAny<Session>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("Tài khoản không tồn tại.", result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.UserNotFound.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.UserNotFound.Description, result.Errors.First().Description);
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

            // Tạo User có mật khẩu đúng trong DB
            var mockUser = AccountTestData.GetValidAccount(email, correctPassword);

            // Tạo Command gửi lên mật khẩu SAI
            var command = AccountTestData.GetLoginCommand(email, wrongInputPassword);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(mockUser);

            // Setup Config giới hạn đăng nhập sai
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("LOGIN_FAILED_LIMIT"))
                .ReturnsAsync("5");
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("LOGIN_LOCKOUT_DURATION_LEVEL_1"))
                .ReturnsAsync("300");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Mật khẩu không chính xác.", result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.WrongPassword.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.WrongPassword.Description, result.Errors.First().Description);

            // Verify UpdateUser được gọi để tăng FailedLoginCount
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==========================================
        // CASE 4: TÀI KHOẢN BỊ KHÓA (BANNED)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsBanned()
        {
            // Arrange
            var email = "banned@tokki.vn";
            var password = "password123";

            // Lấy User bị khóa từ TestData
            var mockUser = AccountTestData.GetBannedAccount(email, password);
            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(mockUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("Tài khoản của bạn đã bị khóa vĩnh viễn. Vui lòng liên hệ CSKH.", result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.AccountBanned.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.AccountBanned.Description, result.Errors.First().Description);
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

            var mockUser = AccountTestData.GetValidAccount(email, password);
            // Đặt thời gian khóa trong tương lai (giờ Việt Nam + 7)
            mockUser.LockedUntil = DateTime.UtcNow.AddHours(7).AddMinutes(10);

            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(mockUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("Tài khoản đang bị tạm khóa", result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.AccountLocked.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.AccountLocked.Description, result.Errors.First().Description);
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

            var mockUser = AccountTestData.GetValidAccount(email, password);
            mockUser.FailedLoginCount = 3; // Đã đăng nhập sai 3 lần trước đó

            var command = AccountTestData.GetLoginCommand(email, password);

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(mockUser);

            _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<EntityAccount>(), It.IsAny<DateTime>()))
                .Returns("fake-jwt-token");

            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>()))
                .Returns("session-id-123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);

            // Verify UpdateUser được gọi để reset FailedLoginCount về 0
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.Is<EntityAccount>(u => u.FailedLoginCount == 0)), Times.Once);
        }
    }
}