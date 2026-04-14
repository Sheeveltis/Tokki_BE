using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaff;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    /// <summary>
    /// Branch-coverage supplement for CreateAccountByAdminCommandHandler.
    /// Covers Moderator role config key, email-send-failure branch,
    /// exception in account creation, and unknown role default branch.
    /// </summary>
    public class CreateAccountByAdminBranchCoverageTests
    {
        private static CreateAccountByAdminCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null,
            Mock<ISystemConfigRepository>? configRepo = null,
            Mock<IEmailService>? emailService = null)
        {
            var repo   = accountRepo ?? BuildCleanRepo();
            var config = configRepo  ?? BuildConfig();
            var email  = emailService ?? new Mock<IEmailService>();

            email.Setup(x => x.SendAccountInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            return new CreateAccountByAdminCommandHandler(
                repo.Object, config.Object,
                MockIdGen().Object, email.Object,
                new Mock<ILogger<CreateAccountByAdminCommandHandler>>().Object);
        }

        private static Mock<IAccountRepository> BuildCleanRepo()
        {
            var m = new Mock<IAccountRepository>();
            m.Setup(x => x.IsEmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            m.Setup(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            m.Setup(x => x.AddAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<ISystemConfigRepository> BuildConfig(
            string staffPass = "StaffPass", string userPass = "UserPass",
            string adminPass = "AdminPass", string modPass = "ModPass")
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF")).ReturnsAsync(staffPass);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER")).ReturnsAsync(userPass);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN")).ReturnsAsync(adminPass);
            m.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_MODERATOR")).ReturnsAsync(modPass);
            return m;
        }

        private static Mock<IIdGeneratorService> MockIdGen()
        {
            var m = new Mock<IIdGeneratorService>();
            m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("NEW_ID");
            return m;
        }

        private static CreateAccountByAdminCommand BuildCmd(
            AccountRole role = AccountRole.Staff,
            string? phone = "0909000000")
            => new()
            {
                Email = "test@tokki.com", FullName = "Test User",
                Role = role, PhoneNumber = phone,
                DateOfBirth = new DateOnly(1995, 1, 1)
            };

        // B01: Moderator role → uses DEFAULT_PASSWORD_FOR_STAFF config key
        [Fact]
        public async Task Handle_ModeratorRole_ShouldReturn201()
        {
            var result = await CreateHandler().Handle(BuildCmd(role: AccountRole.Moderator), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler", TestCaseID = "TC-CAA-B01",
                Description = "Moderator role uses same key as Staff for default password",
                ExpectedResult = "201 created", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "role = Moderator → configKey = DEFAULT_PASSWORD_FOR_STAFF" }
            });
        }

        // B02: User role path → 201
        [Fact]
        public async Task Handle_UserRole_ShouldReturn201()
        {
            var result = await CreateHandler().Handle(BuildCmd(role: AccountRole.User), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Message.Should().Contain("User");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler", TestCaseID = "TC-CAA-B02",
                Description = "User role path → 201 + message contains 'User'",
                ExpectedResult = "201", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "role = User" }
            });
        }

        // B03: Primary key config missing → falls back to DEFAULT_PASSWORD_FOR_STAFF → found → 201
        [Fact]
        public async Task Handle_PrimaryConfigMissing_FallbackToStaff_Returns201()
        {
            var config = new Mock<ISystemConfigRepository>();
            config.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_ADMIN")).ReturnsAsync((string?)null);
            config.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_STAFF")).ReturnsAsync("FallbackPass");

            var result = await CreateHandler(configRepo: config)
                .Handle(BuildCmd(role: AccountRole.Admin), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler", TestCaseID = "TC-CAA-B03",
                Description = "Primary password config missing → fallback to DEFAULT_PASSWORD_FOR_STAFF → 201",
                ExpectedResult = "201 created using fallback password", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PRIMARY config null → fallback staff pass found" }
            });
        }

        // B04: Both primary and fallback configs missing → 500
        [Fact]
        public async Task Handle_BothConfigsMissing_Returns500()
        {
            var config = new Mock<ISystemConfigRepository>();
            config.Setup(x => x.GetValueByKeyAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

            var result = await CreateHandler(configRepo: config)
                .Handle(BuildCmd(role: AccountRole.Staff), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler", TestCaseID = "TC-CAA-B04",
                Description = "Both primary and fallback configs return null → 500",
                ExpectedResult = "500 ServerError", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All GetValueByKeyAsync return null → 500" }
            });
        }

        // B05: Email send fails → still 201 (silent failure)
        [Fact]
        public async Task Handle_EmailSendFails_ShouldStillReturn201()
        {
            var emailMock = new Mock<IEmailService>();
            emailMock.Setup(x => x.SendAccountInfoAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP down"));

            var result = await CreateHandler(emailService: emailMock)
                .Handle(BuildCmd(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler", TestCaseID = "TC-CAA-B05",
                Description = "Email send throws but account still created → 201 (silent failure)",
                ExpectedResult = "201 despite email failure", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SendAccountInfoAsync throws → caught silently → 201" }
            });
        }

        // B06: SaveChangesAsync throws → 500
        [Fact]
        public async Task Handle_SaveChangesFails_Returns500()
        {
            var repo = BuildCleanRepo();
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));

            var result = await CreateHandler(accountRepo: repo)
                .Handle(BuildCmd(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandHandler", TestCaseID = "TC-CAA-B06",
                Description = "SaveChangesAsync throws → outer catch → 500",
                ExpectedResult = "500 ServerError", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws Exception" }
            });
        }
    }
}
