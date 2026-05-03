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
using Tokki.Application.Common.Models;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.FacebookLogin;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class FacebookCompleteRegistrationCommandHandlerTests : IDisposable
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<ISocialLoginRepository> _socialLoginRepoMock = new();
        private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtMock = new();
        private readonly Mock<IIdGeneratorService> _idMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<ILogger<FacebookCompleteRegistrationCommandHandler>> _loggerMock = new();
        
        private readonly FacebookAuthSettings _fbSettings = new() { AppId = "TEST_APP_ID", AppSecret = "TEST_SECRET" };
        private readonly HttpClient _originalHttpClient;

        public FacebookCompleteRegistrationCommandHandlerTests()
        {
            _originalHttpClient = FacebookCompleteRegistrationCommandHandler._httpClient;
            
            _idMock.Setup(x => x.Generate(It.IsAny<int>())).Returns("generated-id");
            _jwtMock.Setup(x => x.GenerateToken(It.IsAny<Account>(), It.IsAny<DateTime>())).Returns("test-token");
            _systemConfigRepoMock.Setup(x => x.GetValueByKeyAsync("DEFAULT_PASSWORD_FOR_USER"))
                                 .ReturnsAsync("Default@123");
                                 
            var optsMock = new Mock<IOptions<FacebookAuthSettings>>();
            optsMock.Setup(x => x.Value).Returns(_fbSettings);
        }

        public void Dispose()
        {
            // Restore original static http client
            FacebookCompleteRegistrationCommandHandler._httpClient = _originalHttpClient;
        }

        private FacebookCompleteRegistrationCommandHandler CreateHandler()
        {
            var optsMock = new Mock<IOptions<FacebookAuthSettings>>();
            optsMock.Setup(x => x.Value).Returns(_fbSettings);

            return new FacebookCompleteRegistrationCommandHandler(
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

        private void SetupMockHttpClient(bool isDebugValid, bool isMeValid, string fbId = "fb-123", string email = "fb@test.com")
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            // Mock Debug Token Response
            var debugResponse = new
            {
                data = new
                {
                    app_id = "TEST_APP_ID",
                    is_valid = isDebugValid,
                    user_id = fbId,
                    expires_at = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
                }
            };

            var meResponse = new
            {
                id = fbId,
                email = email,
                name = "FB User"
            };

            handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(debugResponse))
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = isMeValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                    Content = new StringContent(JsonSerializer.Serialize(meResponse))
                });

            FacebookCompleteRegistrationCommandHandler._httpClient = new HttpClient(handlerMock.Object);
        }

        // FB_Complete_Registration_01 | A | Empty Access Token -> 401
        [Fact]
        public async Task Handle_EmptyToken_ShouldReturnUnauthorized()
        {
            var handler = CreateHandler();
            var result = await handler.Handle(new FacebookCompleteRegistrationCommand { AccessToken = "", FacebookId = "fb", Email = "e" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Account - FB Complete Registration", new TestCaseDetail
            {
                FunctionGroup = "FB Complete Registration",
                TestCaseID = "FB_Complete_Registration_01",
                Description = "Empty access token fails early",
                ExpectedResult = "401 InvalidFacebookToken",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AccessToken empty" }
            });
        }

        // FB_Complete_Registration_02 | A | Invalid Facebook Token -> 401
        [Fact]
        public async Task Handle_InvalidToken_ShouldReturnUnauthorized()
        {
            SetupMockHttpClient(isDebugValid: false, isMeValid: false);
            var handler = CreateHandler();
            
            var result = await handler.Handle(new FacebookCompleteRegistrationCommand { AccessToken = "bad-token", FacebookId = "fb-123", Email = "fb@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Account - FB Complete Registration", new TestCaseDetail
            {
                FunctionGroup = "FB Complete Registration",
                TestCaseID = "FB_Complete_Registration_02",
                Description = "Invalid Facebook token via debug_token endpoint fails",
                ExpectedResult = "401 InvalidFacebookToken",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "API Validation failure" }
            });
        }

        // FB_Complete_Registration_03 | A | Existing Account Merge - Locked User -> 403
        [Fact]
        public async Task Handle_MergeAccount_LockedUser_ShouldReturnForbidden()
        {
            SetupMockHttpClient(isDebugValid: true, isMeValid: true);
            var existingUser = new Account { UserId = "u1", LockedUntil = DateTime.UtcNow.AddHours(8) }; // 8 hours locked since UtcNow+7 used inside
            _accountRepoMock.Setup(x => x.GetByEmailAsync("fb@test.com")).ReturnsAsync(existingUser);
            
            var handler = CreateHandler();
            var cmd = new FacebookCompleteRegistrationCommand { AccessToken = "tok", FacebookId = "fb-123", Email = "fb@test.com", IsComfirmToMergeAcc = true };
            
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Account - FB Complete Registration", new TestCaseDetail
            {
                FunctionGroup = "FB Complete Registration",
                TestCaseID = "FB_Complete_Registration_03",
                Description = "Merge into a locked account fails with 403",
                ExpectedResult = "403 AccountLocked",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil > now" }
            });
        }

        // FB_Complete_Registration_04 | A | Existing Account Merge - Not Confirmed -> 409
        [Fact]
        public async Task Handle_MergeAccount_NotConfirmed_ShouldReturn409()
        {
            SetupMockHttpClient(isDebugValid: true, isMeValid: true);
            var existingUser = new Account { UserId = "u1", Status = AccountStatus.Active };
            _accountRepoMock.Setup(x => x.GetByEmailAsync("fb@test.com")).ReturnsAsync(existingUser);
            
            var handler = CreateHandler();
            var cmd = new FacebookCompleteRegistrationCommand { AccessToken = "tok", FacebookId = "fb-123", Email = "fb@test.com", IsComfirmToMergeAcc = false };
            
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Account - FB Complete Registration", new TestCaseDetail
            {
                FunctionGroup = "FB Complete Registration",
                TestCaseID = "FB_Complete_Registration_04",
                Description = "Email exists but user hasn't confirmed merge",
                ExpectedResult = "409 MergeAccountRequered",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsComfirmToMergeAcc = false" }
            });
        }

        // FB_Complete_Registration_05 | N | Existing Account Merge - Confirmed -> 200 Success
        [Fact]
        public async Task Handle_MergeAccount_Confirmed_ShouldReturnSuccess()
        {
            SetupMockHttpClient(isDebugValid: true, isMeValid: true);
            var existingUser = new Account { UserId = "u1", Status = AccountStatus.Active };
            _accountRepoMock.Setup(x => x.GetByEmailAsync("fb@test.com")).ReturnsAsync(existingUser);
            
            var handler = CreateHandler();
            var cmd = new FacebookCompleteRegistrationCommand { AccessToken = "tok", FacebookId = "fb-123", Email = "fb@test.com", IsComfirmToMergeAcc = true };
            
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Token.Should().Be("test-token");
            _socialLoginRepoMock.Verify(x => x.AddAsync(It.IsAny<SocialLogin>()), Times.Once);

            QACollector.LogTestCase("Account - FB Complete Registration", new TestCaseDetail
            {
                FunctionGroup = "FB Complete Registration",
                TestCaseID = "FB_Complete_Registration_05",
                Description = "Succesfully merges FB with existing account",
                ExpectedResult = "200 Success + Token",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsComfirmToMergeAcc = true" }
            });
        }

        // FB_Complete_Registration_06 | N | New Account Creation -> Creates and sends email -> 200 Success
        [Fact]
        public async Task Handle_NewAccount_ShouldCreateAndReturnSuccess()
        {
            SetupMockHttpClient(isDebugValid: true, isMeValid: true);
            _accountRepoMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);
            
            var handler = CreateHandler();
            var cmd = new FacebookCompleteRegistrationCommand { AccessToken = "tok", FacebookId = "fb-123", Name = "A", Email = "fb@test.com" };
            
            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            
            _accountRepoMock.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
            _emailMock.Verify(x => x.SendFacebookAccountInfoAsync("fb@test.com", "A", "fb@test.com", "Default@123"), Times.Once);

            QACollector.LogTestCase("Account - FB Complete Registration", new TestCaseDetail
            {
                FunctionGroup = "FB Complete Registration",
                TestCaseID = "FB_Complete_Registration_06",
                Description = "Succesfully creates new account parsing FB details",
                ExpectedResult = "200 Success + sends Email",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No existing email" }
            });
        }
    }
}
