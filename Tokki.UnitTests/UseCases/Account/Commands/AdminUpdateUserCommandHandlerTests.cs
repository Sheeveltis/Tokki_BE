using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.AdminUpdateUser;
using Tokki.Domain.Enums;
using Xunit;

// Alias để tránh lỗi namespace/type trùng tên "Account"
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class AdminUpdateUserCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly AdminUpdateUserCommandHandler _handler;

        public AdminUpdateUserCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _handler = new AdminUpdateUserCommandHandler(_mockAccountRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var command = new AdminUpdateUserCommand
            {
                AdminId = "admin-01",
                TargetUserId = "user-not-found",
                FullName = "Any",
                Role = AccountRole.User,
                Status = AccountStatus.Active
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync((AccountEntity?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserNotFoundById.Code);

            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockAccountRepo.Verify(x => x.IsPhoneNumberUsedByOtherUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_PhoneNumberDuplicated()
        {
            // Arrange
            var command = new AdminUpdateUserCommand
            {
                AdminId = "admin-01",
                TargetUserId = "user-01",
                FullName = "New Name",
                PhoneNumber = "0999999999",
                Role = AccountRole.User,
                Status = AccountStatus.Active
            };

            var existingUser = new AccountEntity
            {
                UserId = command.TargetUserId,
                FullName = "Old Name",
                PhoneNumber = "0888888888",
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync(existingUser);

            _mockAccountRepo.Setup(x => x.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber!, command.TargetUserId))
                            .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.PhoneNumberDuplicated.Code);

            _mockAccountRepo.Verify(x => x.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber!, command.TargetUserId), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_StatusIsInactive()
        {
            // Arrange
            var command = new AdminUpdateUserCommand
            {
                AdminId = "admin-01",
                TargetUserId = "user-01",
                FullName = "New Name",
                PhoneNumber = null, // tránh đi vào check duplicate
                Role = AccountRole.User,
                Status = AccountStatus.Inactive
            };

            var existingUser = new AccountEntity
            {
                UserId = command.TargetUserId,
                FullName = "Old Name",
                PhoneNumber = "0888888888",
                Status = AccountStatus.Active,
                Role = AccountRole.User
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync(existingUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountInvalidStatusTransition.Code);

            _mockAccountRepo.Verify(x => x.IsPhoneNumberUsedByOtherUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid_And_UpdateUser()
        {
            // Arrange
            var command = new AdminUpdateUserCommand
            {
                AdminId = "admin-01",
                TargetUserId = "user-01",
                FullName = "Updated Name",
                PhoneNumber = "0912345678",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                AvatarUrl = "https://example.com/a.jpg",
                Role = AccountRole.Admin,
                Status = AccountStatus.Active
            };

            var existingUser = new AccountEntity
            {
                UserId = command.TargetUserId,
                FullName = "Old Name",
                PhoneNumber = "0888888888",
                Role = AccountRole.User,
                Status = AccountStatus.Active,
                AvatarUrl = null,
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-2),
                DateOfBirth = DateTime.UtcNow.AddHours(7).AddYears(-30)
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync(existingUser);

            _mockAccountRepo.Setup(x => x.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber!, command.TargetUserId))
                            .ReturnsAsync(false);

            AccountEntity? updatedEntity = null;
            _mockAccountRepo.Setup(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()))
                            .Callback<AccountEntity>(u => updatedEntity = u)
                            .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Vì handler dùng Success(string) => chuỗi thường nằm ở Data (không phải Message)
            result.Data.Should().Be($"Admin {command.AdminId} đã cập nhật người dùng {command.TargetUserId} thành công!");

            _mockAccountRepo.Verify(x => x.GetByIdAsync(command.TargetUserId), Times.Once);
            _mockAccountRepo.Verify(x => x.IsPhoneNumberUsedByOtherUserAsync(command.PhoneNumber!, command.TargetUserId), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            updatedEntity.Should().NotBeNull();

            updatedEntity!.FullName.Should().Be(command.FullName);
            updatedEntity.PhoneNumber.Should().Be(command.PhoneNumber);
            updatedEntity.AvatarUrl.Should().Be(command.AvatarUrl);
            updatedEntity.Role.Should().Be(command.Role);
            updatedEntity.Status.Should().Be(command.Status);

            updatedEntity.DateOfBirth.Should().Be(command.DateOfBirth!.Value.ToDateTime(TimeOnly.MinValue));

            updatedEntity.UpdatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Handle_Should_NotCheckDuplicatePhone_When_PhoneNumberIsNullOrSame()
        {
            // Arrange: PhoneNumber null => không check duplicate, không update phone
            var command = new AdminUpdateUserCommand
            {
                AdminId = "admin-01",
                TargetUserId = "user-01",
                FullName = "Updated Name",
                PhoneNumber = null,
                Role = AccountRole.User,
                Status = AccountStatus.Active
            };

            var existingUser = new AccountEntity
            {
                UserId = command.TargetUserId,
                FullName = "Old Name",
                PhoneNumber = "0888888888",
                Role = AccountRole.User,
                Status = AccountStatus.Active
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(command.TargetUserId))
                            .ReturnsAsync(existingUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _mockAccountRepo.Verify(x => x.IsPhoneNumberUsedByOtherUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
