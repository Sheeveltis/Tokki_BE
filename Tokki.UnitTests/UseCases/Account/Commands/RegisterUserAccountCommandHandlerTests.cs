using Moq;
using Xunit;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityAccount = Tokki.Domain.Entities.Account;
using Tokki.Application.UseCases.Accounts.Commands.Register;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
// Nếu command của bạn đang nằm sai namespace (Blogs.Commands.CreateBlog) thì giữ dòng dưới.
// using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;

namespace Tokki.UnitTests.UseCases.Account.Commands
{
    public class RegisterUserAccountCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGenerator;
        private readonly Mock<IValidator<RegisterUserAccountCommand>> _mockValidator;
        private readonly Mock<ILogger<RegisterUserAccountCommandHandler>> _mockLogger;

        private readonly RegisterUserAccountCommandHandler _handler;

        public RegisterUserAccountCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
            _mockValidator = new Mock<IValidator<RegisterUserAccountCommand>>();
            _mockLogger = new Mock<ILogger<RegisterUserAccountCommandHandler>>();

            // Default validator: valid
            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Default repo actions: thành công (tránh await null)
            _mockAccountRepo
                .Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .Returns(Task.CompletedTask);

            _mockAccountRepo
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _handler = new RegisterUserAccountCommandHandler(
                _mockAccountRepo.Object,
                _mockIdGenerator.Object,
                _mockLogger.Object,
                _mockValidator.Object
            );
        }
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

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            _mockAccountRepo.Setup(r => r.IsPhoneNumberExistsAsync(command.PhoneNumber))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("User123");

            // Capture entity được add vào DB
            EntityAccount? capturedAccount = null;
            _mockAccountRepo
                .Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .Callback<EntityAccount>(acc => capturedAccount = acc)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("Account registration successful", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("User123", result.Data);

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(capturedAccount);
            Assert.Equal("User123", capturedAccount!.UserId);
            Assert.Equal(command.Email, capturedAccount.Email);
            Assert.Equal(command.FullName, capturedAccount.FullName);
            Assert.Equal(command.PhoneNumber, capturedAccount.PhoneNumber);
            Assert.Equal(AccountStatus.Active, capturedAccount.Status);
            Assert.Equal(AccountRole.User, capturedAccount.Role);

            // Verify hash đúng (gọi ngoài Expression Tree => không lỗi optional args)
            Assert.True(BCrypt.Net.BCrypt.Verify(command.Password, capturedAccount.PasswordHash));
        }


        // ==========================================
        // CASE 2: ĐĂNG KÝ THÀNH CÔNG - KHÔNG CÓ SỐ ĐIỆN THOẠI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenPhoneNumberIsNull()
        {
            var command = new RegisterUserAccountCommand
            {
                Email = "nophone@tokki.vn",
                Password = "Password123!",
                FullName = "No Phone User",
                PhoneNumber = null,
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25))
            };

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("User456");

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("Account registration successful", result.Message);

            _mockAccountRepo.Verify(r => r.IsPhoneNumberExistsAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==========================================
        // CASE 3: LỖI - EMAIL ĐÃ TỒN TẠI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenEmailExists()
        {
            var command = new RegisterUserAccountCommand
            {
                Email = "exist@tokki.vn",
                Password = "Password123!",
                FullName = "Existing User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal(AppErrors.EmailDuplicated.Description, result.Message);

            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.EmailDuplicated.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.EmailDuplicated.Description, result.Errors.First().Description);

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockAccountRepo.Verify(r => r.IsPhoneNumberExistsAsync(It.IsAny<string>()), Times.Never);
        }

        // ==========================================
        // CASE 4: LỖI - SỐ ĐIỆN THOẠI ĐÃ TỒN TẠI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPhoneNumberExists()
        {
            var command = new RegisterUserAccountCommand
            {
                Email = "newuser@tokki.vn",
                Password = "Password123!",
                FullName = "New User",
                PhoneNumber = "0987654321",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            _mockAccountRepo.Setup(r => r.IsPhoneNumberExistsAsync(command.PhoneNumber))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Description, result.Message);

            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Description, result.Errors.First().Description);

            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Never);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldIgnoreValidator_EvenWhenValidatorReturnsErrors()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "invalid-email",
                Password = "",
                FullName = "",
                PhoneNumber = "abc",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            // Setup validator trả lỗi (nhưng handler sẽ không gọi)
            var validationResult = new ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("Email", "Invalid email format."),
                new ValidationFailure("Password", "'Password' cannot be blank.")
            });

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Vì handler bỏ qua validator, cần setup repo để đi đến success
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            // PhoneNumber không null nên handler sẽ gọi IsPhoneNumberExistsAsync
            _mockAccountRepo.Setup(r => r.IsPhoneNumberExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("UserIgnoreValidation");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert: Với code handler hiện tại, kết quả vẫn success (không validate)
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("Account registration successful", result.Message);
            Assert.Equal("UserIgnoreValidation", result.Data);

            // Quan trọng: validator không được gọi
            _mockValidator.Verify(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()), Times.Never);

            _mockAccountRepo.Verify(r => r.IsEmailExistsAsync(It.IsAny<string>()), Times.Once);
            _mockAccountRepo.Verify(r => r.IsPhoneNumberExistsAsync(It.IsAny<string>()), Times.Once);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        // ==========================================
        // CASE 6: HANDLER BỎ QUA VALIDATION - PASSWORD RỖNG (KHÔNG SỬA CODE CŨ)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldIgnoreValidator_WhenPasswordIsEmpty()
        {
            // Arrange
            var command = new RegisterUserAccountCommand
            {
                Email = "test@tokki.vn",
                Password = "",
                FullName = "Test User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            // Validator trả lỗi (nhưng handler sẽ không gọi)
            var validationResult = new ValidationResult(new List<ValidationFailure>
    {
        new ValidationFailure("Password", "'Password' cannot be blank.")
    });

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Vì handler bỏ qua validator nên setup repo để đi đến success
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("UserEmptyPass");

            // Nếu ctor bạn chưa setup default AddAsync/SaveChangesAsync thì setup ở đây
            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert: với code handler hiện tại => vẫn success
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("Account registration successful", result.Message);
            Assert.Equal("UserEmptyPass", result.Data);

            // Validator không được gọi
            _mockValidator.Verify(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()), Times.Never);

            // Repo vẫn bị gọi/lưu
            _mockAccountRepo.Verify(r => r.IsEmailExistsAsync(It.IsAny<string>()), Times.Once);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==========================================
        // CASE 7: HANDLER BỎ QUA VALIDATION (NHIỀU LỖI) - KHÔNG SỬA CODE CŨ
        // ==========================================
        [Fact]
        public async Task Handle_ShouldIgnoreValidator_WhenMultipleValidationErrors()
        {
            // Arrange
            var command = new RegisterUserAccountCommand(); // Email = "", Password = "", ...

            var validationResult = new ValidationResult(new List<ValidationFailure>
    {
        new ValidationFailure("Email", "'Email' cannot be empty."),
        new ValidationFailure("Password", "'Password' cannot be blank."),
        new ValidationFailure("FullName", "'Full name' cannot be blank.")
    });

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Vì handler bỏ qua validator nên phải setup repo để handler đi tiếp
            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("UserIgnoreValidationMany");

            // AddAsync/SaveChangesAsync nếu bạn chưa setup default trong ctor thì setup ở đây
            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .Returns(Task.CompletedTask);
            _mockAccountRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert: với code handler hiện tại, vẫn success (không validate)
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("Account registration successful", result.Message);
            Assert.Equal("UserIgnoreValidationMany", result.Data);

            // Validator không được gọi
            _mockValidator.Verify(v => v.ValidateAsync(It.IsAny<RegisterUserAccountCommand>(), It.IsAny<CancellationToken>()), Times.Never);

            // Repo vẫn bị gọi
            _mockAccountRepo.Verify(r => r.IsEmailExistsAsync(It.IsAny<string>()), Times.Once);
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<EntityAccount>()), Times.Once);
        }


        // ==========================================
        // CASE 8: LỖI SERVER (Exception khi AddAsync)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenDatabaseErrorOccurs()
        {
            var command = new RegisterUserAccountCommand
            {
                Email = "error@tokki.vn",
                Password = "Password123!",
                FullName = "Error User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("UserError");

            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .ThrowsAsync(new Exception("DB connection failed"));

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal(AppErrors.ServerError.Description, result.Message);

            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.ServerError.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.ServerError.Description, result.Errors.First().Description);

            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ==========================================
        // CASE 9: LỖI SERVER (Exception khi SaveChanges)
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenSaveChangesThrowsException()
        {
            var command = new RegisterUserAccountCommand
            {
                Email = "saveerror@tokki.vn",
                Password = "Password123!",
                FullName = "Save Error User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
            };

            _mockAccountRepo.Setup(r => r.IsEmailExistsAsync(command.Email))
                .ReturnsAsync(false);

            _mockIdGenerator.Setup(i => i.GenerateCustom(It.IsAny<int>()))
                .Returns("UserSaveError");

            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<EntityAccount>()))
                .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Save changes failed"));

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal(AppErrors.ServerError.Description, result.Message);

            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.ServerError.Code, result.Errors.First().Code);
        }
    }
}
