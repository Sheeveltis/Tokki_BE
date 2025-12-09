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
using EntityAccount  = Tokki.Domain.Entities.Account;
using Tokki.Application.UseCases.Accounts.Queries.Login;

namespace Tokki.UnitTests.UseCases.Accounts 
{
    public class LoginCommandHandlerTests : AccountTestBase
    {
        // Mock riêng biệt của Use Case này 
        private readonly Mock<IValidator<LoginCommand>> _mockValidator;

        private readonly LoginCommandHandler _handler;

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

            _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<EntityAccount>()))
                .Returns("fake-jwt-token");

            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>()))
                .Returns("session-id-123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
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

            // Setup Config giới hạn đăng nhập sai (để tránh lỗi null config)
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("LOGIN_FAILED_LIMIT"))
                .ReturnsAsync("5");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Mật khẩu không chính xác.", result.Message);

            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Once);
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
        }
    }
}