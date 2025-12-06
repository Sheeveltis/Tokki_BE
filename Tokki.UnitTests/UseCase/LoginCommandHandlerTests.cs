using Moq;
using Xunit;
using FluentValidation;
using FluentValidation.Results;
using Tokki.Application.UseCases.Accounts.Queries.Login;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace Tokki.UnitTests.UseCases.Accounts
{
    public class LoginCommandHandlerTests
    {
        // 1. Khai báo các đối tượng giả lập (Mock)
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        // 👇 [QUAN TRỌNG] Thêm dòng này để khai báo biến
        private readonly Mock<ISystemConfigRepository> _mockSystemConfigRepo;
        private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
        private readonly Mock<IIdGeneratorService> _mockIdGenerator;
        private readonly Mock<IValidator<LoginCommand>> _mockValidator;

        // Đối tượng chính cần test
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            // 2. Initialize Mocks
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockSystemConfigRepo = new Mock<ISystemConfigRepository>(); // Khởi tạo Mock Config
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
            _mockValidator = new Mock<IValidator<LoginCommand>>();

            // Setup Validator luôn đúng
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // 3. Inject Mocks vào Handler thật
            _handler = new LoginCommandHandler(
                _mockAccountRepo.Object,
                _mockSystemConfigRepo.Object, // 👇 [QUAN TRỌNG] Truyền Object Config vào đây
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
            var email = "test@tokki.vn";
            var password = "Password123!";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var mockUser = new Account
            {
                UserId = "User001",
                Email = email,
                PasswordHash = passwordHash,
                Status = AccountStatus.Active,
                Role = AccountRole.User,
                FullName = "Test User"
            };

            // Setup Repo trả về User
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(mockUser);

            // Setup Token & ID
            _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<Account>()))
                .Returns("fake-jwt-token");

            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>()))
                .Returns("session-id-123");

            var command = new LoginCommand { Email = email, Password = password };

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
            _mockAccountRepo.Setup(x => x.GetByEmailAsync("wrong@email.com"))
                .ReturnsAsync((Account)null);

            var command = new LoginCommand { Email = "wrong@email.com", Password = "123" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode); // Bạn đã đổi code thành 404 cho user null
            Assert.Equal("Tài khoản không tồn tại.", result.Message); // Message mới
        }

        // ==========================================
        // CASE 3: SAI MẬT KHẨU
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPasswordIsWrong()
        {
            // Arrange
            var correctPassHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass");
            var mockUser = new Account
            {
                Email = "test@email.com",
                PasswordHash = correctPassHash,
                FailedLoginCount = 0
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync("test@email.com"))
                .ReturnsAsync(mockUser);

            // Mock config trả về limit mặc định (để tránh lỗi null reference trong handler)
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("LOGIN_FAILED_LIMIT"))
                .ReturnsAsync("5");

            var command = new LoginCommand { Email = "test@email.com", Password = "WrongPassword" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Mật khẩu không chính xác.", result.Message);

            // Kiểm tra xem có gọi UpdateUserAsync để tăng biến đếm sai không
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);
        }

        // ==========================================
        // CASE 4: TÀI KHOẢN BỊ KHÓA (BANNED)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAccountIsBanned()
        {
            // Arrange
            var passHash = BCrypt.Net.BCrypt.HashPassword("123");
            var mockUser = new Account
            {
                Email = "banned@email.com",
                PasswordHash = passHash,
                Status = AccountStatus.Banned
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync("banned@email.com"))
                .ReturnsAsync(mockUser);

            var command = new LoginCommand { Email = "banned@email.com", Password = "123" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.StatusCode);
        }
    }
}