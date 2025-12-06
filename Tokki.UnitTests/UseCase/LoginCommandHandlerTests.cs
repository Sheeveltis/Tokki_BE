using Moq;
using Xunit;
using FluentValidation;
using FluentValidation.Results; // Cần thiết để tạo kết quả ValidationResult
using Tokki.Application.UseCases.Accounts.Queries.Login;
using Tokki.Application.UseCases.Accounts.Commands.Login; // Namespace chứa LoginCommand
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
        private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
        private readonly Mock<IIdGeneratorService> _mockIdGenerator;
        private readonly Mock<IValidator<LoginCommand>> _mockValidator; // ✅ THÊM MOCK VALIDATOR

        // Đối tượng chính cần test
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            // 2. Khởi tạo Mock
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
            _mockValidator = new Mock<IValidator<LoginCommand>>(); // ✅ KHỞI TẠO MOCK VALIDATOR

            // ✅ SETUP QUAN TRỌNG: Mặc định Validator luôn trả về "Hợp lệ"
            // Nếu thiếu dòng này, test sẽ bị lỗi NullReference hoặc Valid=false
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // 3. Tiêm Mock vào Handler thật
            _handler = new LoginCommandHandler(
                _mockAccountRepo.Object,
                _mockJwtGenerator.Object,
                _mockIdGenerator.Object,
                _mockValidator.Object // ✅ TRUYỀN VALIDATOR VÀO ĐÂY
            );
        }

        // ==========================================
        // CASE 1: ĐĂNG NHẬP THÀNH CÔNG
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenCredentialsAreCorrect()
        {
            // Arrange (Chuẩn bị dữ liệu giả)
            var email = "test@tokki.vn";
            var password = "Password123!";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password); // Hash mật khẩu chuẩn

            var mockUser = new Account
            {
                UserId = "User001",
                Email = email,
                PasswordHash = passwordHash, // Giả lập DB đã lưu hash
                Status = AccountStatus.Active,
                Role = AccountRole.User,
                FullName = "Test User"
            };

            // Giả lập Repository: Khi tìm Email -> Trả về mockUser
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(mockUser);

            // Giả lập JWT Generator: Trả về token fake
            _mockJwtGenerator.Setup(x => x.GenerateToken(It.IsAny<Account>()))
                .Returns("fake-jwt-token");

            // Giả lập IdGenerator: Trả về ID session fake
            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>()))
                .Returns("session-id-123");

            var command = new LoginCommand { Email = email, Password = password };

            // Act (Chạy hàm cần test)
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert (Kiểm tra kết quả)
            Assert.True(result.IsSuccess); // Phải thành công
            Assert.Equal("fake-jwt-token", result.Data.Token); // Token phải đúng

            // Kiểm tra xem hàm lưu Session có được gọi 1 lần không?
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
                .ReturnsAsync((Account)null); // Trả về null

            var command = new LoginCommand { Email = "wrong@email.com", Password = "123" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Tài khoản hoặc mật khẩu không chính xác.", result.Message);
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
                PasswordHash = correctPassHash
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync("test@email.com"))
                .ReturnsAsync(mockUser);

            var command = new LoginCommand { Email = "test@email.com", Password = "WrongPassword" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
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
                Status = AccountStatus.Banned // Set trạng thái bị khóa
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync("banned@email.com"))
                .ReturnsAsync(mockUser);

            var command = new LoginCommand { Email = "banned@email.com", Password = "123" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.StatusCode); // Phải trả về 403 Forbidden
        }
    }
}