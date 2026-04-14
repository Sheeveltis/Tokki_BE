using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.FacebookLogin;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class FacebookLoginBranchCoverageTests : IDisposable
    {
        private readonly Mock<IAccountRepository>      _accountRepo = new();
        private readonly Mock<ISocialLoginRepository>  _socialRepo  = new();
        private readonly Mock<ISystemConfigRepository> _configRepo  = new();
        private readonly Mock<IJwtTokenGenerator>      _jwt         = new();
        private readonly Mock<IIdGeneratorService>     _idGen       = new();
        private readonly Mock<IEmailService>           _email       = new();
        private readonly HttpClient _original;

        public FacebookLoginBranchCoverageTests()
        {
            _original = FacebookLoginCommandHandler._httpClient;
            _idGen.Setup(x => x.Generate(It.IsAny<int>())).Returns("gen-id");
            _jwt.Setup(x => x.GenerateToken(It.IsAny<Account>(), It.IsAny<DateTime>())).Returns("jwt");
            _configRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER")).ReturnsAsync("Pass@123");
            _configRepo.Setup(x => x.GetValueByKeyAsync("TOKEN_EXPIRATION_MINUTES")).ReturnsAsync("60");
        }

        public void Dispose() => FacebookLoginCommandHandler._httpClient = _original;

        private FacebookLoginCommandHandler CreateHandler()
        {
            var opts = new Mock<IOptions<FacebookAuthSettings>>();
            opts.Setup(x => x.Value).Returns(new FacebookAuthSettings { AppId = "APP_ID", AppSecret = "SECRET" });
            return new FacebookLoginCommandHandler(
                _accountRepo.Object, _socialRepo.Object, _configRepo.Object,
                _jwt.Object, _idGen.Object, _email.Object, opts.Object,
                new Mock<ILogger<FacebookLoginCommandHandler>>().Object);
        }

        private static void SetupHttp(bool valid = true, string fbId = "fb-123", string? email = "fb@test.com")
        {
            var debugBody = JsonSerializer.Serialize(new
            {
                data = new { app_id = "APP_ID", is_valid = valid, user_id = fbId, expires_at = 9999999999L }
            });
            object meBody = email != null
                ? (object)new { id = fbId, email = email, name = "FB User" }
                : new { id = fbId, name = "FB User" };
            var meBodyStr = JsonSerializer.Serialize(meBody);

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                    { StatusCode = HttpStatusCode.OK, Content = new StringContent(debugBody) })
                .ReturnsAsync(new HttpResponseMessage
                    { StatusCode = HttpStatusCode.OK, Content = new StringContent(meBodyStr) });

            FacebookLoginCommandHandler._httpClient = new HttpClient(handler.Object);
        }

        // B01: SocialLogin exists, account null → 404
        [Fact]
        public async Task Handle_SocialLoginExists_AccountNull_Returns404()
        {
            SetupHttp();
            _socialRepo.Setup(x => x.GetByProviderAsync("facebook", "fb-123"))
                       .ReturnsAsync(new SocialLogin { UserId = "u1" });
            _accountRepo.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync((Account?)null);

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok" }, CancellationToken.None);

            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B01",
                Description = "Social login found but linked account null → 404",
                ExpectedResult = "404", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // B02: SocialLogin exists, account Locked → 403
        [Fact]
        public async Task Handle_SocialLoginExists_AccountLocked_Returns403()
        {
            SetupHttp();
            _socialRepo.Setup(x => x.GetByProviderAsync("facebook", "fb-123"))
                       .ReturnsAsync(new SocialLogin { UserId = "u1" });
            _accountRepo.Setup(x => x.GetByIdAsync("u1"))
                        .ReturnsAsync(new Account { UserId = "u1", Status = AccountStatus.Active, LockedUntil = DateTime.UtcNow.AddHours(5) });

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B02",
                Description = "Social login found, account temp-locked → 403",
                ExpectedResult = "403 AccountLocked", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil > now in social-login path" }
            });
        }

        // B03: SocialLogin exists, account Inactive → 403
        [Fact]
        public async Task Handle_SocialLoginExists_AccountInactive_Returns403()
        {
            SetupHttp();
            _socialRepo.Setup(x => x.GetByProviderAsync("facebook", "fb-123"))
                       .ReturnsAsync(new SocialLogin { UserId = "u1" });
            _accountRepo.Setup(x => x.GetByIdAsync("u1"))
                        .ReturnsAsync(new Account { UserId = "u1", Status = AccountStatus.Inactive });

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B03",
                Description = "Social login found, account Inactive → 403",
                ExpectedResult = "403 AccountInActive", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "account.Status == Inactive in social-login path" }
            });
        }

        // B04: No social login, email exists, account Locked → 403
        [Fact]
        public async Task Handle_EmailExists_AccountLocked_Returns403()
        {
            SetupHttp();
            _socialRepo.Setup(x => x.GetByProviderAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync((SocialLogin?)null);
            _accountRepo.Setup(x => x.GetByEmailAsync("fb@test.com"))
                        .ReturnsAsync(new Account { UserId = "u1", Status = AccountStatus.Active, LockedUntil = DateTime.UtcNow.AddHours(3) });

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B04",
                Description = "Email exists path, account locked → 403",
                ExpectedResult = "403 AccountLocked", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil > now in email-exists path" }
            });
        }

        // B05: No social login, email exists, account Inactive → 403
        [Fact]
        public async Task Handle_EmailExists_AccountInactive_Returns403()
        {
            SetupHttp();
            _socialRepo.Setup(x => x.GetByProviderAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync((SocialLogin?)null);
            _accountRepo.Setup(x => x.GetByEmailAsync("fb@test.com"))
                        .ReturnsAsync(new Account { UserId = "u1", Status = AccountStatus.Inactive });

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok", IsComfirmToMergeAcc = true }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B05",
                Description = "Email exists path, account Inactive → 403",
                ExpectedResult = "403 AccountInActive", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Inactive in email-exists path" }
            });
        }

        // B06: New account, DEFAULT_PASSWORD missing → 500
        [Fact]
        public async Task Handle_NewAccount_MissingDefaultPass_Returns500()
        {
            SetupHttp();
            _socialRepo.Setup(x => x.GetByProviderAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync((SocialLogin?)null);
            _accountRepo.Setup(x => x.GetByEmailAsync("fb@test.com")).ReturnsAsync((Account?)null);
            _configRepo.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER")).ReturnsAsync((string?)null);

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B06",
                Description = "New account path, DEFAULT_PASSWORD_FOR_USER null → 500",
                ExpectedResult = "500 ServerError", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetValueByKeyAsync returns null" }
            });
        }

        // B07: New account, email send fails → 200 with warning
        [Fact]
        public async Task Handle_NewAccount_EmailFails_Returns200WithWarning()
        {
            SetupHttp();
            _socialRepo.Setup(x => x.GetByProviderAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync((SocialLogin?)null);
            _accountRepo.Setup(x => x.GetByEmailAsync("fb@test.com")).ReturnsAsync((Account?)null);
            _email.Setup(x => x.SendFacebookAccountInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .ThrowsAsync(new Exception("SMTP fail"));

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("gửi email thất bại");

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B07",
                Description = "New account created but email send throws → 200 + warning message",
                ExpectedResult = "200 + message contains email failure note", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SendFacebookAccountInfoAsync throws" }
            });
        }

        // B08: Exception thrown by ValidateFacebookTokenAsync → 401
        [Fact]
        public async Task Handle_HttpCallThrows_Returns401()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));
            FacebookLoginCommandHandler._httpClient = new HttpClient(handlerMock.Object);

            var result = await CreateHandler().Handle(new FacebookLoginCommand { AccessToken = "tok" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Account - Login", new TestCaseDetail
            {
                FunctionGroup = "FacebookLoginCommandHandler", TestCaseID = "TC-FBL-B08",
                Description = "HttpClient throws → outer catch returns 401",
                ExpectedResult = "401 InvalidFacebookToken", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HttpRequestException in ValidateFacebookTokenAsync" }
            });
        }
    }
}
