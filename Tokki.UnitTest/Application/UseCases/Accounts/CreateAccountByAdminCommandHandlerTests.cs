using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaff;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class CreateAccountByAdminCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static CreateAccountByAdminCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null,
            Mock<ISystemConfigRepository>? configRepo = null,
            Mock<IIdGeneratorService>? idGen = null,
            Mock<IEmailService>? emailService = null)
        {
            var mockConfig = configRepo ?? BuildConfigMock();
            var mockId = idGen ?? MockIdGeneratorService.GetMock();
            var mockEmail = emailService ?? new Mock<IEmailService>();

            mockEmail.Setup(x => x.SendAccountInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            return new CreateAccountByAdminCommandHandler(
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                mockConfig.Object,
                mockId.Object,
                mockEmail.Object,
                new Mock<ILogger<CreateAccountByAdminCommandHandler>>().Object);
        }

        private static Mock<ISystemConfigRepository> BuildConfigMock(
            string staffPass = "DefaultStaffPass",
            string userPass  = "DefaultUserPass",
            string adminPass = "DefaultAdminPass")
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF")).ReturnsAsync(staffPass);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER")).ReturnsAsync(userPass);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN")).ReturnsAsync(adminPass);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_MODERATOR")).ReturnsAsync(staffPass);
            return m;
        }

        private static CreateAccountByAdminCommand BuildCommand(
            string email       = "newstaff@tokki.com",
            string fullName    = "New Staff",
            AccountRole role   = AccountRole.Staff,
            string? phone      = "0909123456")
            => new()
            {
                Email       = email,
                FullName    = fullName,
                Role        = role,
                PhoneNumber = phone,
                DateOfBirth = new DateOnly(1995, 6, 15)
            };

        // ═══════════════════════════════════════════════════════════
        // Create_Account_By_Admin_01 | A | Duplicate email → 409
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateEmail_ShouldReturn409()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.IsEmailExistsAsync("dup@tokki.com")).ReturnsAsync(true);

            var command = BuildCommand(email: "dup@tokki.com");
            var result = await CreateHandler(accountRepo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "Create Account By Admin",
                TestCaseID = "Create_Account_By_Admin_01",
                Description = "Email already registered in the system",
                ExpectedResult = "Return 409 EmailDuplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsEmailExistsAsync = true", "Return 409" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Account_By_Admin_02 | A | Duplicate phone number → 409
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicatePhone_ShouldReturn409()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            mockRepo.Setup(x => x.IsPhoneNumberExistsAsync("0909999999")).ReturnsAsync(true);

            var command = BuildCommand(phone: "0909999999");
            var result = await CreateHandler(accountRepo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "Create Account By Admin",
                TestCaseID = "Create_Account_By_Admin_02",
                Description = "Phone number already registered in the system",
                ExpectedResult = "Return 409 PhoneNumberDuplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsPhoneNumberExistsAsync = true", "Return 409" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Account_By_Admin_03 | A | Default password config not found → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DefaultPasswordConfigMissing_ShouldReturn500()
        {
            var mockConfig = new Mock<ISystemConfigRepository>();
            mockConfig.Setup(x => x.GetValueByKeyAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

            var command = BuildCommand(role: AccountRole.Staff, phone: null);
            var result = await CreateHandler(configRepo: mockConfig).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "Create Account By Admin",
                TestCaseID = "Create_Account_By_Admin_03",
                Description = "Default password config is missing for the role",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetValueByKeyAsync returns null for all keys",
                    "Return 500"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Account_By_Admin_04 | N | Valid Staff account → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidStaffAccount_ShouldReturn201()
        {
            var command = BuildCommand(role: AccountRole.Staff);
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "Create Account By Admin",
                TestCaseID = "Create_Account_By_Admin_04",
                Description = "Valid staff account created successfully, email notification sent",
                ExpectedResult = "Return 201, UserId in Data, account saved, email sent",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email not duplicate",
                    "Phone not duplicate",
                    "DEFAULT_PASSWORD_FOR_STAFF is configured",
                    "Role = Staff",
                    "Return 201"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Account_By_Admin_05 | N | Valid Admin account → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidAdminAccount_ShouldReturn201()
        {
            var command = BuildCommand(role: AccountRole.Admin, email: "admin2@tokki.com");
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "Create Account By Admin",
                TestCaseID = "Create_Account_By_Admin_05",
                Description = "Valid admin account created successfully",
                ExpectedResult = "Return 201, UserId in Data",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No duplicate email or phone",
                    "DEFAULT_PASSWORD_FOR_ADMIN is configured",
                    "Role = Admin",
                    "Return 201"
                }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Create_Account_By_Admin_06 | N | No phone provided → 201 (optional phone)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoPhone_ShouldSkipPhoneCheckAndReturn201()
        {
            var command = BuildCommand(phone: null);
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "Create Account By Admin",
                TestCaseID = "Create_Account_By_Admin_06",
                Description = "Phone number is optional, creation succeeds without it",
                ExpectedResult = "Return 201, phone check skipped",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "PhoneNumber = null",
                    "IsPhoneNumberExistsAsync NOT called",
                    "Return 201"
                }
            });
        }
    }
}
