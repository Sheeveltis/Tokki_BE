using Moq;
using Xunit;
using FluentValidation;
using FluentValidation.Results; // Cần để tạo kết quả validate giả
using Microsoft.Extensions.Logging;
using Tokki.Application.UseCases.Accounts.Commands.Register;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using EntityAccount = Tokki.Domain.Entities.Account;
namespace Tokki.UnitTests.UseCases.Account.Commands
{
    public class RegisterUserAccountCommandHandlerTests
    {
        // 1. Khai báo các Mock
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGenerator;
        private readonly Mock<IValidator<RegisterUserAccountCommand>> _mockValidator; // Mock Validator
        private readonly Mock<ILogger<RegisterUserAccountCommandHandler>> _mockLogger;

        private readonly RegisterUserAccountCommandHandler _handler;

        public RegisterUserAccountCommandHandlerTests()
        {
            // 2. Khởi tạo Mock
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
            _mockValidator = new Mock<IValidator<RegisterUserAccountCommand>>();
            _mockLogger = new Mock<ILogger<RegisterUserAccountCommandHandler>>();

            // 3. Inject Mock vào Handler
            _handler = new RegisterUserAccountCommandHandler(
                _mockAccountRepo.Object,
                _mockIdGenerator.Object,
                _mockLogger.Object,
                _mockValidator.Object // Truyền Validator giả vào
            );
        }

        // ==========================================
        // CASE 1: ĐĂNG KÝ THÀNH CÔNG (Happy Path)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "newuser@tokki.vn",
                Password = "Password123",
                FullName = "New User",
                PhoneNumber = "0987654321",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            // Giả lập Validator: Luôn trả về Hợp lệ (IsValid = true)
            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult()); // List error rỗng = Valid

            // Giả lập: Email chưa tồn tại
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            // Giả lập: Sinh ID
            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("User123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("User123", result.Data);

            // Kiểm tra xem hàm lưu xuống DB có được gọi không
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==========================================
        // CASE 2: LỖI VALIDATION (Dữ liệu không hợp lệ)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenValidationFails()
        {
            // Arrange
            var command = new RegisterUserAccountCommand(); // Dữ liệu rỗng

            // Giả lập Validator: Trả về Lỗi
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Email", "Email không được để trống"),
                new ValidationFailure("Password", "Mật khẩu quá ngắn")
            };
            var validationResult = new ValidationResult(validationFailures);

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Validation.Failed", result.Errors[0].Code);

            // QUAN TRỌNG: Nếu Validate lỗi thì KHÔNG ĐƯỢC gọi xuống DB
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 3: LỖI TRÙNG EMAIL
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenEmailExists()
        {
            // Arrange
            var command = new RegisterUserAccountCommand { Email = "exist@tokki.vn", Password = "123" };

            // Giả lập Validator: Hợp lệ
            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Giả lập: Email ĐÃ tồn tại (return true)
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User.EmailDuplicated", result.Errors[0].Code);

            // Không được lưu DB
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 4: LỖI SERVER (Exception khi lưu DB)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenDatabaseErrorOccurs()
        {
            // Arrange
            var command = new RegisterUserAccountCommand { Email = "error@tokki.vn", Password = "123" };

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            // Giả lập: Khi gọi AddAsync thì ném ra Exception
            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .ThrowsAsync(new Exception("DB connection failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            Assert.Contains("Lỗi hệ thống", result.Message);
        }
    }
}