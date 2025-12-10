using Moq;
using Xunit;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Tokki.Application.UseCases.Accounts.Commands.Register;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityAccount = Tokki.Domain.Entities.Account;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;

namespace Tokki.UnitTests.UseCases.Account.Commands
{
    public class RegisterUserAccountCommandHandlerTests
    {
        // 1. Khai báo các Mock
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGenerator;
        private readonly Mock<IValidator<RegisterUserAccountCommand>> _mockValidator;
        private readonly Mock<ILogger<RegisterUserAccountCommandHandler>> _mockLogger;

        private readonly RegisterUserAccountCommandHandler _handler;

        public RegisterUserAccountCommandHandlerTests()
        {
            // 2. Khởi tạo Mock
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
            _mockValidator = new Mock<IValidator<RegisterUserAccountCommand>>();
            _mockLogger = new Mock<ILogger<RegisterUserAccountCommandHandler>>();

            // 3. Setup Validator mặc định là Valid (có thể override trong từng test)
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // 4. Inject Mock vào Handler
            _handler = new RegisterUserAccountCommandHandler(
                _mockAccountRepo.Object,
                _mockIdGenerator.Object,
                _mockLogger.Object,
                _mockValidator.Object
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
                Password = "Password123!",
                FullName = "New User",
                PhoneNumber = "0987654321",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            // Giả lập: Email chưa tồn tại
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            // Giả lập: Phone chưa tồn tại
            _mockAccountRepo.Setup(r => r.IsPhoneNumberExistsAsync(command.PhoneNumber))
                .ReturnsAsync(false);

            // Giả lập: Sinh ID
            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("User123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("Đăng ký tài khoản thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("User123", result.Data);

            // Kiểm tra xem hàm lưu xuống DB có được gọi không
            _mockAccountRepo.Verify(r => r.AddAsync(It.Is<EntityAccount>(a =>
                a.UserId == "User123" &&
                a.Email == command.Email &&
                a.FullName == command.FullName &&
                a.Status == AccountStatus.Active &&
                a.Role == AccountRole.User
            )), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==========================================
        // CASE 2: ĐĂNG KÝ THÀNH CÔNG - KHÔNG CÓ SỐ ĐIỆN THOẠI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenPhoneNumberIsNull()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "nophone@tokki.vn",
                Password = "Password123!",
                FullName = "No Phone User",
                PhoneNumber = null, // Không có số điện thoại
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25))
            };

            // Giả lập: Email chưa tồn tại
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            // Giả lập: Sinh ID
            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("User456");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("Đăng ký tài khoản thành công", result.Message);

            // Verify KHÔNG gọi IsPhoneNumberExistsAsync vì PhoneNumber là null
            _mockAccountRepo.Verify(r => r.IsPhoneNumberExistsAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Once);
        }

        // ==========================================
        // CASE 3: LỖI - EMAIL ĐÃ TỒN TẠI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenEmailExists()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "exist@tokki.vn",
                Password = "Password123!",
                FullName = "Existing User"
            };

            // Giả lập: Email ĐÃ tồn tại (return true)
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode); // Conflict
            Assert.Equal(AppErrors.EmailDuplicated.Description, result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.EmailDuplicated.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.EmailDuplicated.Description, result.Errors.First().Description);

            // Không được lưu DB
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ==========================================
        // CASE 4: LỖI - SỐ ĐIỆN THOẠI ĐÃ TỒN TẠI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPhoneNumberExists()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "newuser@tokki.vn",
                Password = "Password123!",
                FullName = "New User",
                PhoneNumber = "0987654321" // SĐT đã tồn tại
            };

            // Giả lập: Email chưa tồn tại
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            // Giả lập: Phone ĐÃ tồn tại (return true)
            _mockAccountRepo.Setup(r => r.IsPhoneNumberExistsAsync(command.PhoneNumber))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode); // Conflict
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Description, result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Description, result.Errors.First().Description);

            // Không được lưu DB
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 5: LỖI VALIDATION - EMAIL KHÔNG HỢP LỆ
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenEmailIsInvalid()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "invalid-email", // Email không đúng định dạng
                Password = "Password123!",
                FullName = "Test User"
            };

            // Giả lập Validator: Trả về Lỗi
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Email", "Định dạng email không hợp lệ.")
            };
            var validationResult = new ValidationResult(validationFailures);

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);

            // Kiểm tra Error message từ ValidationVietnameseLanguageManager
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Description.Contains("Định dạng email không hợp lệ"));

            // QUAN TRỌNG: Nếu Validate lỗi thì KHÔNG ĐƯỢC gọi xuống DB
            _mockAccountRepo.Verify(r => r.IsEmailExistsAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 6: LỖI VALIDATION - MẬT KHẨU TRỐNG
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPasswordIsEmpty()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "test@tokki.vn",
                Password = "", // Mật khẩu rỗng
                FullName = "Test User"
            };

            // Giả lập Validator: Trả về Lỗi
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Mật khẩu", "'Mật khẩu' không được để trống.")
            };
            var validationResult = new ValidationResult(validationFailures);

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);

            // Kiểm tra Error message từ ValidationVietnameseLanguageManager
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Description.Contains("không được để trống"));

            // KHÔNG được gọi xuống DB
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 7: LỖI VALIDATION - NHIỀU LỖI CÙNG LÚC
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenMultipleValidationErrors()
        {
            // Arrange
            var command = new RegisterUserAccountCommand(); // Dữ liệu rỗng

            // Giả lập Validator: Trả về nhiều Lỗi
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Email", "'Email' không được để trống."),
                new ValidationFailure("Mật khẩu", "'Mật khẩu' không được để trống."),
                new ValidationFailure("FullName", "'FullName' không được để trống.")
            };
            var validationResult = new ValidationResult(validationFailures);

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);

            // Kiểm tra có nhiều lỗi
            Assert.NotNull(result.Errors);
            Assert.Equal(3, result.Errors.Count);

            // KHÔNG được gọi xuống DB
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 8: LỖI SERVER (Exception khi lưu DB)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenDatabaseErrorOccurs()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "error@tokki.vn",
                Password = "Password123!",
                FullName = "Error User"
            };

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("UserError");

            // Giả lập: Khi gọi AddAsync thì ném ra Exception
            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .ThrowsAsync(new Exception("DB connection failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal(AppErrors.ServerError.Description, result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.ServerError.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.ServerError.Description, result.Errors.First().Description);
        }

        // ==========================================
        // CASE 9: LỖI SERVER (Exception khi SaveChanges)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenSaveChangesThrowsException()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "saveerror@tokki.vn",
                Password = "Password123!",
                FullName = "Save Error User"
            };

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("UserSaveError");

            // AddAsync thành công
            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .Returns(Task.CompletedTask);

            // Nhưng SaveChangesAsync ném Exception
            _mockAccountRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Save changes failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal(AppErrors.ServerError.Description, result.Message);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.ServerError.Code, result.Errors.First().Code);
        }
    }
}