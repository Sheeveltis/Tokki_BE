using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaff;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;
using Tokki.Domain.Enums;
using Xunit;

// Alias tránh lỗi "Account is a namespace but is used like a type"
using AccountEntity = Tokki.Domain.Entities.Account;

namespace Tokki.UnitTests.UseCases.Accounts.Commands
{
    public class CreateAccountByAdminCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<ISystemConfigRepository> _mockSystemConfigRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGenerator;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<CreateAccountByAdminCommandHandler>> _mockLogger;

        private readonly CreateAccountByAdminCommandHandler _handler;

        public CreateAccountByAdminCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockSystemConfigRepo = new Mock<ISystemConfigRepository>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<CreateAccountByAdminCommandHandler>>();

            _handler = new CreateAccountByAdminCommandHandler(
                _mockAccountRepo.Object,
                _mockSystemConfigRepo.Object,
                _mockIdGenerator.Object,
                _mockEmailService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_EmailAlreadyExists()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand
            {
                Email = "exist@tokki.vn",
                FullName = "Exist User",
                PhoneNumber = "0900000000",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Role = AccountRole.Staff
            };

            _mockAccountRepo.Setup(x => x.IsEmailExistsAsync(command.Email))
                            .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Message.Should().Be("Email already exists.");
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailDuplicated.Code);

            _mockAccountRepo.Verify(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>()), Times.Never);
            _mockSystemConfigRepo.Verify(x => x.GetValueByKeyAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.AddAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(x => x.SendAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_PhoneAlreadyExists()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand
            {
                Email = "new@tokki.vn",
                FullName = "New User",
                PhoneNumber = "0900000000",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Role = AccountRole.Staff
            };

            _mockAccountRepo.Setup(x => x.IsEmailExistsAsync(command.Email))
                            .ReturnsAsync(false);

            _mockAccountRepo.Setup(x => x.IsPhoneNumberExistsAsync(command.PhoneNumber!))
                            .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Message.Should().Be("Phone number already exists.");
            result.Errors.Should().Contain(e => e.Code == AppErrors.PhoneNumberDuplicated.Code);

            _mockSystemConfigRepo.Verify(x => x.GetValueByKeyAsync(It.IsAny<string>()), Times.Never);
            _mockAccountRepo.Verify(x => x.AddAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(x => x.SendAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_NotCheckPhone_When_PhoneIsNull()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand
            {
                Email = "nophone@tokki.vn",
                FullName = "No Phone",
                PhoneNumber = null,
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Role = AccountRole.Staff
            };

            _mockAccountRepo.Setup(x => x.IsEmailExistsAsync(command.Email))
                            .ReturnsAsync(false);

            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF"))
                                 .ReturnsAsync("Default@123");

            _mockIdGenerator.Setup(x => x.GenerateCustom(It.IsAny<int>()))
                            .Returns("U-STF-01");

            _mockAccountRepo.Setup(x => x.AddAsync(It.IsAny<AccountEntity>()))
                            .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            _mockEmailService.Setup(x => x.SendAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            _mockAccountRepo.Verify(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DefaultPasswordNotConfigured()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand
            {
                Email = "new@tokki.vn",
                FullName = "New User",
                PhoneNumber = null,
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Role = AccountRole.Staff
            };

            _mockAccountRepo.Setup(x => x.IsEmailExistsAsync(command.Email))
                            .ReturnsAsync(false);

            // Staff -> DEFAULT_PASSWORD_FOR_STAFF, trả null => fallback cũng null
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF"))
                                 .ReturnsAsync((string?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ServerError.Code);
            result.Message.Should().Contain("Configure default password");

            _mockAccountRepo.Verify(x => x.AddAsync(It.IsAny<AccountEntity>()), Times.Never);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(x => x.SendAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_DataIsValid_And_SendEmailOk()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand
            {
                Email = "staff@tokki.vn",
                FullName = "Staff User",
                PhoneNumber = "0912345678",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                Role = AccountRole.Staff
            };

            _mockAccountRepo.Setup(x => x.IsEmailExistsAsync(command.Email))
                            .ReturnsAsync(false);

            _mockAccountRepo.Setup(x => x.IsPhoneNumberExistsAsync(command.PhoneNumber!))
                            .ReturnsAsync(false);

            const string defaultPassword = "Default@123";
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF"))
                                 .ReturnsAsync(defaultPassword);

            _mockIdGenerator.Setup(x => x.GenerateCustom(It.IsAny<int>()))
                            .Returns("U-STF-99");

            AccountEntity? addedEntity = null;
            _mockAccountRepo.Setup(x => x.AddAsync(It.IsAny<AccountEntity>()))
                            .Callback<AccountEntity>(a => addedEntity = a)
                            .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            _mockEmailService.Setup(x => x.SendAccountInfoAsync(command.Email, command.FullName, command.Email, defaultPassword))
                             .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("U-STF-99");
            result.Message.Should().Be("Create a Staff account and email login information successfully.");

            _mockAccountRepo.Verify(x => x.AddAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(x => x.SendAccountInfoAsync(command.Email, command.FullName, command.Email, defaultPassword), Times.Once);

            addedEntity.Should().NotBeNull();
            addedEntity!.UserId.Should().Be("U-STF-99");
            addedEntity.Email.Should().Be(command.Email);
            addedEntity.FullName.Should().Be(command.FullName);
            addedEntity.PhoneNumber.Should().Be(command.PhoneNumber);
            addedEntity.Role.Should().Be(AccountRole.Staff);
            addedEntity.Status.Should().Be(AccountStatus.Active);
            addedEntity.AvatarUrl.Should().BeNull();

            // DateOfBirth được convert sang DateTime (00:00)
            addedEntity.DateOfBirth.Should().Be(command.DateOfBirth.ToDateTime(TimeOnly.MinValue));

            // CreatedAt set theo VN time
            addedEntity.CreatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));

            // Verify hash đúng (làm ngoài Moq expression tree)
            BCrypt.Net.BCrypt.Verify(defaultPassword, addedEntity.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_EvenWhen_SendEmailFails()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand
            {
                Email = "staff2@tokki.vn",
                FullName = "Staff 2",
                PhoneNumber = null,
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-22)),
                Role = AccountRole.Staff
            };

            _mockAccountRepo.Setup(x => x.IsEmailExistsAsync(command.Email))
                            .ReturnsAsync(false);

            const string defaultPassword = "Default@123";
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF"))
                                 .ReturnsAsync(defaultPassword);

            _mockIdGenerator.Setup(x => x.GenerateCustom(It.IsAny<int>()))
                            .Returns("U-STF-02");

            _mockAccountRepo.Setup(x => x.AddAsync(It.IsAny<AccountEntity>()))
                            .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            _mockEmailService.Setup(x => x.SendAccountInfoAsync(command.Email, command.FullName, command.Email, defaultPassword))
                             .ThrowsAsync(new Exception("SMTP down"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("U-STF-02");
            result.Message.Should().Be("Create a Staff account and email login information successfully.");

            _mockAccountRepo.Verify(x => x.AddAsync(It.IsAny<AccountEntity>()), Times.Once);
            _mockAccountRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Email có gọi nhưng bị throw, handler catch và bỏ qua
            _mockEmailService.Verify(x => x.SendAccountInfoAsync(command.Email, command.FullName, command.Email, defaultPassword), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_UseFallbackPassword_When_RoleConfigMissing()
        {
            // Arrange (test fallback: Admin key null -> dùng DEFAULT_PASSWORD_FOR_STAFF)
            var command = new CreateAccountByAdminCommand
            {
                Email = "admin@tokki.vn",
                FullName = "Admin User",
                PhoneNumber = null,
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
                Role = AccountRole.Admin
            };

            _mockAccountRepo.Setup(x => x.IsEmailExistsAsync(command.Email))
                            .ReturnsAsync(false);

            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN"))
                                 .ReturnsAsync((string?)null);

            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF"))
                                 .ReturnsAsync("Fallback@123");

            _mockIdGenerator.Setup(x => x.GenerateCustom(It.IsAny<int>()))
                            .Returns("U-ADM-01");

            _mockAccountRepo.Setup(x => x.AddAsync(It.IsAny<AccountEntity>()))
                            .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            _mockEmailService.Setup(x => x.SendAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("U-ADM-01");
            result.Message.Should().Be("Create an Admin account and email login information successfully.");

            _mockSystemConfigRepo.Verify(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN"), Times.Once);
            _mockSystemConfigRepo.Verify(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF"), Times.Once);
        }
    }
}
