using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.GoogleLogin;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class GoogleLoginCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<ISocialLoginRepository> _socialLoginRepoMock = new();
        private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtMock = new();
        private readonly Mock<IIdGeneratorService> _idMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<ILogger<GoogleLoginCommandHandler>> _loggerMock = new();
        
        private readonly GoogleAuthSettings _googleSettings = new() { ClientIds = new List<string> { "TEST_CLIENT_ID" } };

        public GoogleLoginCommandHandlerTests()
        {
            _idMock.Setup(x => x.Generate(It.IsAny<int>())).Returns("generated-id");
            _jwtMock.Setup(x => x.GenerateToken(It.IsAny<Account>(), It.IsAny<DateTime>())).Returns("test-token");
            _systemConfigRepoMock.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER"))
                                 .ReturnsAsync("Default@123");
            _systemConfigRepoMock.Setup(x => x.GetValueByKeyAsync("TOKEN_EXPIRATION_MINUTES"))
                                 .ReturnsAsync("60");
        }

        private GoogleLoginCommandHandler CreateHandler()
        {
            var optsMock = new Mock<IOptions<GoogleAuthSettings>>();
            optsMock.Setup(x => x.Value).Returns(_googleSettings);

            return new GoogleLoginCommandHandler(
                _accountRepoMock.Object,
                _socialLoginRepoMock.Object,
                _systemConfigRepoMock.Object,
                _jwtMock.Object,
                _idMock.Object,
                _emailMock.Object,
                optsMock.Object,
                _loggerMock.Object
            );
        }

        private OperationResult<LoginResponse>? InvokeCheckAccountStatus(GoogleLoginCommandHandler handler, Account user, DateTime nowLocal)
        {
            var method = typeof(GoogleLoginCommandHandler).GetMethod("CheckAccountStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            return (OperationResult<LoginResponse>?)method?.Invoke(handler, new object[] { user, nowLocal });
        }
        
        private async Task<int> InvokeGetIntConfigAsync(GoogleLoginCommandHandler handler, string key, int defaultValue)
        {
            var method = typeof(GoogleLoginCommandHandler).GetMethod("GetIntConfigAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<int>)method!.Invoke(handler, new object[] { key, defaultValue })!;
            return await task;
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ACC-GGL-01 | A | Handle: Invalid Format Token -> InvalidJwtException -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InvalidGoogleToken_ShouldReturn401()
        {
            var handler = CreateHandler();
            // "invalid" will be rejected instantly by GoogleJsonWebSignature.ValidateAsync doing split('.')
            var result = await handler.Handle(new GoogleLoginCommand { IdToken = "invalid" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "GoogleLoginCommandHandler",
                TestCaseID = "TC-ACC-GGL-01",
                Description = "Verify that an invalid Google ID token format causes the handler to catch an InvalidJwtException and return 401 Unauthorized",
                ExpectedResult = "Returns OperationResult with IsSuccess=false and StatusCode=401",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Google ID token is a malformed string ('invalid') that cannot be parsed as a JWT" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ACC-GGL-02 | A | Handle: Null Token -> ArgumentNullException -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullToken_ShouldCatchExceptionReturn401()
        {
            var handler = CreateHandler();
            var result = await handler.Handle(new GoogleLoginCommand { IdToken = null! }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "GoogleLoginCommandHandler",
                TestCaseID = "TC-ACC-GGL-02",
                Description = "Verify that a null Google ID token causes the handler to catch an exception and return 401 Unauthorized",
                ExpectedResult = "Returns OperationResult with IsSuccess=false and StatusCode=401",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Google ID token is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ACC-GGL-03 | A | CheckAccountStatus: Inactive User
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CheckAccountStatus_Inactive_Returns403()
        {
            var handler = CreateHandler();
            var user = new Account { Status = AccountStatus.Inactive };

            var result = InvokeCheckAccountStatus(handler, user, DateTime.UtcNow);

            result.Should().NotBeNull();
            result!.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.First().Code.Should().Be("Account.InActive");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "GoogleLoginCommandHandler",
                TestCaseID = "TC-ACC-GGL-03",
                Description = "Verify that CheckAccountStatus returns 403 Forbidden when the account status is Inactive",
                ExpectedResult = "Returns OperationResult with StatusCode=403 and error code 'Account.InActive'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account.Status = AccountStatus.Inactive" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ACC-GGL-04 | A | CheckAccountStatus: Banned User
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CheckAccountStatus_Banned_Returns403()
        {
            var handler = CreateHandler();
            var user = new Account { Status = AccountStatus.Banned };

            var result = InvokeCheckAccountStatus(handler, user, DateTime.UtcNow);

            result.Should().NotBeNull();
            result!.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.First().Code.Should().Be("Account.Banned");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "GoogleLoginCommandHandler",
                TestCaseID = "TC-ACC-GGL-04",
                Description = "Verify that CheckAccountStatus returns 403 Forbidden when the account status is Banned",
                ExpectedResult = "Returns OperationResult with StatusCode=403 and error code 'Account.Banned'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account.Status = AccountStatus.Banned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ACC-GGL-05 | A | CheckAccountStatus: Locked User
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CheckAccountStatus_Locked_Returns403()
        {
            var handler = CreateHandler();
            var now = DateTime.UtcNow;
            var user = new Account { Status = AccountStatus.Active, LockedUntil = now.AddMinutes(10) };

            var result = InvokeCheckAccountStatus(handler, user, now);

            result.Should().NotBeNull();
            result!.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.First().Code.Should().Be("Account.Locked");
            result.Message.Should().Contain("10 phút");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "GoogleLoginCommandHandler",
                TestCaseID = "TC-ACC-GGL-05",
                Description = "Verify that CheckAccountStatus returns 403 when the account is temporarily locked (LockedUntil > current time)",
                ExpectedResult = "Returns OperationResult with StatusCode=403, error code 'Account.Locked', and message containing remaining lock time '10 phút'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account.Status = Active, LockedUntil = now + 10 minutes" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ACC-GGL-06 | N | CheckAccountStatus: Active and Not Locked
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void CheckAccountStatus_Active_ReturnsNull()
        {
            var handler = CreateHandler();
            var now = DateTime.UtcNow;
            var user = new Account { Status = AccountStatus.Active, LockedUntil = now.AddMinutes(-10) }; // lock expired

            var result = InvokeCheckAccountStatus(handler, user, now);

            result.Should().BeNull(); // null means successful check

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "GoogleLoginCommandHandler",
                TestCaseID = "TC-ACC-GGL-06",
                Description = "Verify that CheckAccountStatus returns null (no blocking result) when the account is Active and the lock has expired",
                ExpectedResult = "Returns null, indicating the account status check passed successfully",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account.Status = Active, LockedUntil = now - 10 minutes (expired)" }
            });
        }
        
        // ═══════════════════════════════════════════════════════════
        // TC-ACC-GGL-07 | N | GetIntConfigAsync: Falls back to default cleanly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task GetIntConfigAsync_InvalidValue_ReturnsDefault()
        {
            _systemConfigRepoMock.Setup(x => x.GetValueByKeyAsync("BOGUS_KEY")).ReturnsAsync("abc");
            var handler = CreateHandler();
            var result = await InvokeGetIntConfigAsync(handler, "BOGUS_KEY", 99);

            result.Should().Be(99);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "GoogleLoginCommandHandler",
                TestCaseID = "TC-ACC-GGL-07",
                Description = "Verify that GetIntConfigAsync returns the default value when the system config value is not a valid integer",
                ExpectedResult = "Returns default value 99 when config value 'abc' cannot be parsed to int",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SystemConfig key 'BOGUS_KEY' returns non-numeric value 'abc', defaultValue = 99" }
            });
        }
    }
}
