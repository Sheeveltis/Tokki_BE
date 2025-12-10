using Moq;
using Xunit;
using FluentValidation;
using FluentValidation.Results;
using Tokki.Application.UseCases.Accounts.Commands.UpdateProfile;
using Tokki.Application.IRepositories;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityAccount = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class UpdateProfileCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IValidator<UpdateProfileCommand>> _mockValidator;
        private readonly UpdateProfileCommandHandler _handler;

        public UpdateProfileCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockValidator = new Mock<IValidator<UpdateProfileCommand>>();

            // Setup Validator mặc định là Valid
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateProfileCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _handler = new UpdateProfileCommandHandler(_mockAccountRepo.Object);
        }

        // ==========================================
        // CASE 1: CẬP NHẬT THÀNH CÔNG - ĐẦY ĐỦ THÔNG TIN
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenUpdateAllFields()
        {
            // Arrange
            var userId = "User123";
            var command = new UpdateProfileCommand
            {
                UserId = userId,
                FullName = "Updated Name",
                PhoneNumber = "0912345678",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                AvatarUrl = "https://example.com/avatar.jpg"
            };

            var existingUser = new EntityAccount
            {
                UserId = userId,
                Email = "test@tokki.vn",
                FullName = "Old Name",
                PhoneNumber = "0987654321",
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mockAccountRepo.Setup(r => r.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Cập nhật thông tin thành công!", result.Data);

            // Verify UpdateUserAsync được gọi
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.Is<EntityAccount>(u =>
                u.UserId == userId &&
                u.FullName == command.FullName &&
                u.PhoneNumber == command.PhoneNumber &&
                u.AvatarUrl == command.AvatarUrl
            )), Times.Once);

            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==========================================
        // CASE 2: CẬP NHẬT THÀNH CÔNG - CHỈ CẬP NHẬT MỘT SỐ TRƯỜNG
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenUpdatePartialFields()
        {
            // Arrange
            var userId = "User456";
            var command = new UpdateProfileCommand
            {
                UserId = userId,
                FullName = "New Name Only",
                PhoneNumber = null, // Không cập nhật phone
                DateOfBirth = null, // Không cập nhật DOB
                AvatarUrl = null    // Không cập nhật avatar
            };

            var existingUser = new EntityAccount
            {
                UserId = userId,
                Email = "partial@tokki.vn",
                FullName = "Old Name",
                PhoneNumber = "0987654321",
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);

            // Verify chỉ FullName được cập nhật, các trường khác giữ nguyên
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.Is<EntityAccount>(u =>
                u.FullName == "New Name Only" &&
                u.PhoneNumber == "0987654321" // Giữ nguyên
            )), Times.Once);
        }

        // ==========================================
        // CASE 3: LỖI - USERID NULL HOẶC EMPTY
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserIdIsNull()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = null, // UserId null
                FullName = "Test Name"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.UserUnauthorized.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.UserUnauthorized.Description, result.Errors.First().Description);

            // Không được gọi DB
            _mockAccountRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 4: LỖI - USERID EMPTY STRING
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserIdIsEmpty()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = "", // UserId empty
                FullName = "Test Name"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.UserUnauthorized.Code, result.Errors.First().Code);

            // Không được gọi DB
            _mockAccountRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        // ==========================================
        // CASE 5: LỖI - USER KHÔNG TỒN TẠI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = "NonExistentUser",
                FullName = "Test Name"
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(command.UserId))
                .ReturnsAsync((EntityAccount)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.UserNotFoundById.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.UserNotFoundById.Description, result.Errors.First().Description);

            // Không được gọi UpdateUserAsync
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 6: LỖI - SỐ ĐIỆN THOẠI ĐÃ ĐƯỢC DÙNG BỞI USER KHÁC
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPhoneNumberIsUsedByOtherUser()
        {
            // Arrange
            var userId = "User789";
            var command = new UpdateProfileCommand
            {
                UserId = userId,
                PhoneNumber = "0999999999" // SĐT đã được user khác sử dụng
            };

            var existingUser = new EntityAccount
            {
                UserId = userId,
                Email = "test@tokki.vn",
                FullName = "Test User",
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            // SĐT đã được user khác sử dụng
            _mockAccountRepo.Setup(r => r.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);

            // Kiểm tra Error từ AppErrors
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Description, result.Errors.First().Description);

            // Không được gọi UpdateUserAsync
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Never);
        }

        // ==========================================
        // CASE 7: LỖI VALIDATION - SỐ ĐIỆN THOẠI KHÔNG HỢP LỆ
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPhoneNumberIsInvalid()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = "User123",
                PhoneNumber = "abc123xyz" // Chứa ký tự không phải số
            };

            // Giả lập Validator: Trả về Lỗi
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Số điện thoại", "Số điện thoại chỉ được chứa các ký tự số.")
            };
            var validationResult = new ValidationResult(validationFailures);

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);

            // Kiểm tra Error message
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Description.Contains("Số điện thoại chỉ được chứa các ký tự số"));

            // Không được gọi DB
            _mockAccountRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        // ==========================================
        // CASE 8: LỖI VALIDATION - HỌ TÊN QUÁ DÀI
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenFullNameTooLong()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = "User123",
                FullName = new string('A', 300) // Quá 255 ký tự
            };

            // Giả lập Validator: Trả về Lỗi
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Họ và tên", "'Họ và tên' tối đa 255 ký tự. Bạn đã nhập 300 ký tự.")
            };
            var validationResult = new ValidationResult(validationFailures);

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);

            // Kiểm tra Error message
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Description.Contains("tối đa 255 ký tự"));
        }

        // ==========================================
        // CASE 9: LỖI VALIDATION - NGÀY SINH KHÔNG HỢP LỆ
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenDateOfBirthIsInFuture()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = "User123",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(1)) // Ngày sinh trong tương lai
            };

            // Giả lập Validator: Trả về Lỗi
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Ngày sinh", "Ngày sinh không hợp lệ (phải nhỏ hơn ngày hiện tại).")
            };
            var validationResult = new ValidationResult(validationFailures);

            _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);

            // Kiểm tra Error message
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Description.Contains("Ngày sinh không hợp lệ"));
        }

        // ==========================================
        // CASE 10: CẬP NHẬT THÀNH CÔNG - UPDATEDTT ĐƯỢC SET
        // ==========================================
        [Fact]
        public async Task Handle_ShouldSetUpdatedAt_WhenUpdateSuccessful()
        {
            // Arrange
            var userId = "UserTime";
            var command = new UpdateProfileCommand
            {
                UserId = userId,
                FullName = "New Name"
            };

            var existingUser = new EntityAccount
            {
                UserId = userId,
                Email = "time@tokki.vn",
                FullName = "Old Name",
                UpdatedAt = null, // Chưa có UpdatedAt
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify UpdatedAt được set
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.Is<EntityAccount>(u =>
                u.UpdatedAt != null &&
                u.UpdatedAt.Value.Date == DateTime.UtcNow.AddHours(7).Date
            )), Times.Once);
        }
    }
}