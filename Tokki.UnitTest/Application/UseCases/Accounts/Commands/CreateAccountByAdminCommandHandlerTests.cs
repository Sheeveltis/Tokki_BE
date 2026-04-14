using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaff;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class CreateAccountByAdminCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        private readonly Mock<ISystemConfigRepository> _systemConfigRepositoryMock = new();
        private readonly Mock<IIdGeneratorService> _idGeneratorServiceMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<ILogger<CreateAccountByAdminCommandHandler>> _loggerMock = new();

        private CreateAccountByAdminCommandHandler CreateHandler()
        {
            return new CreateAccountByAdminCommandHandler(
                _accountRepositoryMock.Object,
                _systemConfigRepositoryMock.Object,
                _idGeneratorServiceMock.Object,
                _emailServiceMock.Object,
                _loggerMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CABA-01 | A | Email Exists -> 409 Conflict
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmailExists_ShouldReturn409()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand { Email = "exist@test.com", Role = AccountRole.Admin };
            _accountRepositoryMock.Setup(x => x.IsEmailExistsAsync(command.Email)).ReturnsAsync(true);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.EmailDuplicated.Code);

            // Excel Log
            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler",
                TestCaseID = "TC-CABA-01",
                Description = "Returns 409 when email already exists",
                ExpectedResult = "Return 409 and EmailDuplicated error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsEmailExistsAsync returns true" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CABA-02 | A | Phone Number Exists -> 409 Conflict
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PhoneExists_ShouldReturn409()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand { Email = "new@test.com", PhoneNumber = "123456789", Role = AccountRole.Admin };
            _accountRepositoryMock.Setup(x => x.IsEmailExistsAsync(command.Email)).ReturnsAsync(false);
            _accountRepositoryMock.Setup(x => x.IsPhoneNumberExistsAsync(command.PhoneNumber)).ReturnsAsync(true);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.PhoneNumberDuplicated.Code);

            // Excel Log
            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler",
                TestCaseID = "TC-CABA-02",
                Description = "Returns 409 when phone number already exists",
                ExpectedResult = "Return 409 and PhoneNumberDuplicated error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsPhoneNumberExistsAsync returns true" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CABA-03 | A | Config Missing Default Password -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ConfigMissing_ShouldReturn500()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand { Email = "new@test.com", Role = AccountRole.User };
            _accountRepositoryMock.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync(It.IsAny<string>())).ReturnsAsync((string?)null); // Missing config
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler",
                TestCaseID = "TC-CABA-03",
                Description = "Returns 500 when default password config is missing",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetValueByKeyAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CABA-04 | N | Success Creates User Role -> 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SuccessUser_ShouldReturn201()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand { Email = "new@test.com", Role = AccountRole.User };
            _accountRepositoryMock.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER")).ReturnsAsync("pass123");
            _idGeneratorServiceMock.Setup(x => x.GenerateCustom(10)).Returns("ID123");
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("ID123");
            _accountRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler",
                TestCaseID = "TC-CABA-04",
                Description = "Successfully creates User role account",
                ExpectedResult = "Return 201",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid request", "Role is User" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CABA-05 | B | Success but Email Sends Fails -> 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmailSendFails_ShouldStillReturn201()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand { Email = "new@test.com", Role = AccountRole.Staff };
            _accountRepositoryMock.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF")).ReturnsAsync("pass123");
            _idGeneratorServiceMock.Setup(x => x.GenerateCustom(10)).Returns("ID123");
            _emailServiceMock.Setup(x => x.SendAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Error"));

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            _accountRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler",
                TestCaseID = "TC-CABA-05",
                Description = "Account creates successfully even if email sending throws exception",
                ExpectedResult = "Return 201 without failure",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "EmailService throws exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CABA-06 | E | Database Add Throws Exception -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseException_ShouldReturn500()
        {
            // Arrange
            var command = new CreateAccountByAdminCommand { Email = "new@test.com", Role = AccountRole.Admin };
            _accountRepositoryMock.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN")).ReturnsAsync("pass123");
            _accountRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Account>())).ThrowsAsync(new Exception("DB Error"));

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler",
                TestCaseID = "TC-CABA-06",
                Description = "Returns 500 when database insertion fails",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repository AddAsync throws exception" }
            });
        }
    }
}
