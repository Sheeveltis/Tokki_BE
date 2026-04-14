using Moq;
using Xunit;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.UpdateProfile;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using EntityAccount = Tokki.Domain.Entities.Account;
using Tokki.Application.Common.Models;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class UpdateProfileCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly UpdateProfileCommandHandler _handler;

        public UpdateProfileCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();

            // Tránh await null
            _mockAccountRepo
                .Setup(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()))
                .Returns(Task.CompletedTask);

            _mockAccountRepo
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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

            _mockAccountRepo.Setup(r => r.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber!, userId))
                .ReturnsAsync(false);

            EntityAccount? updatedUser = null;
            _mockAccountRepo.Setup(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()))
                .Callback<EntityAccount>(u => updatedUser = u)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);

            // Với OperationResult.Success("...", 200) => chuỗi thường nằm ở Data
            Assert.Equal("Updated information successfully!", result.Data);

            _mockAccountRepo.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _mockAccountRepo.Verify(r => r.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber!, userId), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(updatedUser);
            Assert.Equal(userId, updatedUser!.UserId);
            Assert.Equal(command.FullName, updatedUser.FullName);
            Assert.Equal(command.PhoneNumber, updatedUser.PhoneNumber);
            Assert.Equal(command.AvatarUrl, updatedUser.AvatarUrl);
            Assert.NotNull(updatedUser.UpdatedAt);
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
                PhoneNumber = null,
                DateOfBirth = null,
                AvatarUrl = null
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

            EntityAccount? updatedUser = null;
            _mockAccountRepo.Setup(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()))
                .Callback<EntityAccount>(u => updatedUser = u)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);

            _mockAccountRepo.Verify(r => r.IsPhoneNumberUsedByOtherUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Once);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(updatedUser);
            Assert.Equal("New Name Only", updatedUser!.FullName);
            Assert.Equal("0987654321", updatedUser.PhoneNumber); // giữ nguyên
        }

        // ==========================================
        // CASE 3: LỖI - USERID NULL
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserIdIsNull()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = null,
                FullName = "Test Name"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);

            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.UserUnauthorized.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.UserUnauthorized.Description, result.Errors.First().Description);

            _mockAccountRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Never);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ==========================================
        // CASE 4: LỖI - USERID EMPTY
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenUserIdIsEmpty()
        {
            // Arrange
            var command = new UpdateProfileCommand
            {
                UserId = "",
                FullName = "Test Name"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.UserUnauthorized.Code, result.Errors.First().Code);

            _mockAccountRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Never);
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

            _mockAccountRepo.Setup(r => r.GetByIdAsync(command.UserId!))
                .ReturnsAsync((EntityAccount)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.UserNotFoundById.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.UserNotFoundById.Description, result.Errors.First().Description);

            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Never);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ==========================================
        // CASE 6: LỖI - SĐT ĐÃ ĐƯỢC DÙNG BỞI USER KHÁC
        // ==========================================
        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPhoneNumberIsUsedByOtherUser()
        {
            // Arrange
            var userId = "User789";
            var command = new UpdateProfileCommand
            {
                UserId = userId,
                PhoneNumber = "0999999999"
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

            _mockAccountRepo.Setup(r => r.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber!, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Code, result.Errors.First().Code);
            Assert.Equal(AppErrors.PhoneNumberDuplicated.Description, result.Errors.First().Description);

            _mockAccountRepo.Verify(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()), Times.Never);
            _mockAccountRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ==========================================
        // CASE 7/8/9: VALIDATION -> TEST VALIDATOR RIÊNG (VÌ HANDLER KHÔNG VALIDATE)
        // ==========================================
        [Fact]
        public async Task Validator_ShouldFail_WhenPhoneNumberIsInvalid()
        {
            var validator = new UpdateProfileCommandValidator();
            var command = new UpdateProfileCommand
            {
                UserId = "User123",
                PhoneNumber = "abc123xyz"
            };

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Phone numbers must contain only numeric characters."));
        }

        [Fact]
        public async Task Validator_ShouldFail_WhenFullNameTooLong()
        {
            var validator = new UpdateProfileCommandValidator();
            var command = new UpdateProfileCommand
            {
                UserId = "User123",
                FullName = new string('A', 300)
            };

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "FullName");
        }

        [Fact]
        public async Task Validator_ShouldFail_WhenDateOfBirthIsInFuture()
        {
            var validator = new UpdateProfileCommandValidator();
            var command = new UpdateProfileCommand
            {
                UserId = "User123",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(1))
            };

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Invalid date of birth"));
        }

        // ==========================================
        // CASE 10: UPDATEDAT ĐƯỢC SET
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
                UpdatedAt = null,
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            EntityAccount? updatedUser = null;
            _mockAccountRepo.Setup(r => r.UpdateUserAsync(It.IsAny<EntityAccount>()))
                .Callback<EntityAccount>(u => updatedUser = u)
                .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(updatedUser);
            Assert.NotNull(updatedUser!.UpdatedAt);

            // Tránh flaky: chỉ cần UpdatedAt gần thời điểm hiện tại
            Assert.True((updatedUser.UpdatedAt.Value - nowVn).Duration() < TimeSpan.FromMinutes(2));
        }
    }
}
