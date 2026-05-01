using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class SendEmailVerificationOtpCommandHandlerTests
    {
        private static SendGeneralOtpCommandHandler CreateHandler(
            Mock<IRedisService>? redis = null,
            Mock<IAccountRepository>? account = null,
            Mock<IEmailService>? email = null,
            Mock<IValidator<SendEmailVerificationOtpCommand>>? validator = null,
            Mock<ISystemConfigRepository>? sysConfig = null)
        {
            var mockValidator = validator ?? BuildPassValidator();
            var mockConfig    = sysConfig ?? BuildDefaultConfig("300");
            return new SendGeneralOtpCommandHandler(
                (redis   ?? new Mock<IRedisService>()).Object,
                (account ?? new Mock<IAccountRepository>()).Object,
                (email   ?? new Mock<IEmailService>()).Object,
                mockValidator.Object,
                mockConfig.Object);
        }

        private static Mock<IValidator<SendEmailVerificationOtpCommand>> BuildPassValidator()
        {
            var v = new Mock<IValidator<SendEmailVerificationOtpCommand>>();
            v.Setup(x => x.ValidateAsync(It.IsAny<SendEmailVerificationOtpCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ValidationResult());
            return v;
        }

        private static Mock<ISystemConfigRepository> BuildDefaultConfig(string value)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS")).ReturnsAsync(value);
            return m;
        }

        // TC-01: Email already exists (Active) → 400 EmailAlreadyExists
        [Fact]
        public async Task Handle_EmailAlreadyExists_ShouldReturn400()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("existing@test.com"))
                   .ReturnsAsync(new Account { Email = "existing@test.com", Status = AccountStatus.Active });

            var result = await CreateHandler(account: account)
                .Handle(new SendEmailVerificationOtpCommand { Email = "existing@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("OTP - Send Email Verify", new TestCaseDetail
            {
                FunctionGroup = "SendEmailVerificationOtp", TestCaseID = "SendEmailVerificationOtp_01",
                Description = "Email already registered (Active) → 400 EmailAlreadyExists",
                ExpectedResult = "Return 400", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "account != null && Active => 400" }
            });
        }

        // TC-02: Email exists but Banned → 400 AccountUnavailable
        [Fact]
        public async Task Handle_EmailBanned_ShouldReturn400AccountUnavailable()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("banned@test.com"))
                   .ReturnsAsync(new Account { Email = "banned@test.com", Status = AccountStatus.Banned });

            var result = await CreateHandler(account: account)
                .Handle(new SendEmailVerificationOtpCommand { Email = "banned@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("OTP - Send Email Verify", new TestCaseDetail
            {
                FunctionGroup = "SendEmailVerificationOtp", TestCaseID = "SendEmailVerificationOtp_02",
                Description = "Email exists but Banned → 400 AccountUnavailable",
                ExpectedResult = "Return 400 AccountUnavailable", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "account.Status == Banned => 400 AccountUnavailable" }
            });
        }

        // TC-03: Rate limit active → 429
        [Fact]
        public async Task Handle_RateLimitActive_ShouldReturn429()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("new@test.com")).ReturnsAsync((Account?)null);

            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP_RL:VerifyEmail:new@test.com")).ReturnsAsync("1");
            redis.Setup(x => x.GetTtlAsync("OTP_RL:VerifyEmail:new@test.com"))
                 .ReturnsAsync(TimeSpan.FromSeconds(45));

            var result = await CreateHandler(redis: redis, account: account)
                .Handle(new SendEmailVerificationOtpCommand { Email = "new@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(429);

            QACollector.LogTestCase("OTP - Send Email Verify", new TestCaseDetail
            {
                FunctionGroup = "SendEmailVerificationOtp", TestCaseID = "SendEmailVerificationOtp_03",
                Description = "Rate limit key exists in Redis → 429 TooManyRequests",
                ExpectedResult = "Return 429", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "rateLimitEntry != null => 429" }
            });
        }

        // TC-04: Happy path → OTP stored, rate-limit key stored
        [Fact]
        public async Task Handle_NewEmail_ShouldStoreOtpAndRateLimit()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("new@test.com")).ReturnsAsync((Account?)null);

            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP_RL:VerifyEmail:new@test.com")).ReturnsAsync((string?)null);

            var result = await CreateHandler(redis: redis, account: account)
                .Handle(new SendEmailVerificationOtpCommand { Email = "new@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // OTP key stored
            redis.Verify(x => x.SetAsync(
                "OTP:VerifyEmail:new@test.com",
                It.Is<string>(v => v.Contains("OtpCode")),
                It.IsAny<TimeSpan>()), Times.Once);

            // Rate-limit key stored for 60s
            redis.Verify(x => x.SetAsync(
                "OTP_RL:VerifyEmail:new@test.com",
                "1",
                TimeSpan.FromSeconds(60)), Times.Once);

            QACollector.LogTestCase("OTP - Send Email Verify", new TestCaseDetail
            {
                FunctionGroup = "SendEmailVerificationOtp", TestCaseID = "SendEmailVerificationOtp_04",
                Description = "New email → OTP key + rate-limit key stored in Redis, Return 200",
                ExpectedResult = "Return 200, both Redis keys set", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "no existing account, no rate limit => store OTP + RL key" }
            });
        }

        // TC-05: SendEmail throws → OTP and RL keys deleted, return 400
        [Fact]
        public async Task Handle_EmailServiceThrows_ShouldDeleteKeysAndReturn400()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("new@test.com")).ReturnsAsync((Account?)null);

            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP_RL:VerifyEmail:new@test.com")).ReturnsAsync((string?)null);

            var email = new Mock<IEmailService>();
            email.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .ThrowsAsync(new Exception("SMTP error"));

            var result = await CreateHandler(redis: redis, account: account, email: email)
                .Handle(new SendEmailVerificationOtpCommand { Email = "new@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Keys must be cleaned up
            redis.Verify(x => x.DeleteAsync("OTP:VerifyEmail:new@test.com"), Times.Once);
            redis.Verify(x => x.DeleteAsync("OTP_RL:VerifyEmail:new@test.com"), Times.Once);

            QACollector.LogTestCase("OTP - Send Email Verify", new TestCaseDetail
            {
                FunctionGroup = "SendEmailVerificationOtp", TestCaseID = "SendEmailVerificationOtp_05",
                Description = "Email service throws → both Redis keys deleted, Return 400",
                ExpectedResult = "Return 400, both keys deleted", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SendEmailAsync throws => DeleteAsync x2 => 400" }
            });
        }

        // TC-06: Config-based TTL used for OTP lifetime
        [Fact]
        public async Task Handle_CustomConfigTtl_ShouldUseConfiguredLifetime()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("new@test.com")).ReturnsAsync((Account?)null);

            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP_RL:VerifyEmail:new@test.com")).ReturnsAsync((string?)null);

            var sysConfig = new Mock<ISystemConfigRepository>();
            sysConfig.Setup(x => x.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS")).ReturnsAsync("600");

            await CreateHandler(redis: redis, account: account, sysConfig: sysConfig)
                .Handle(new SendEmailVerificationOtpCommand { Email = "new@test.com" }, CancellationToken.None);

            // 600 seconds TTL used for the OTP key
            redis.Verify(x => x.SetAsync(
                "OTP:VerifyEmail:new@test.com",
                It.IsAny<string>(),
                TimeSpan.FromSeconds(600)), Times.Once);

            QACollector.LogTestCase("OTP - Send Email Verify", new TestCaseDetail
            {
                FunctionGroup = "SendEmailVerificationOtp", TestCaseID = "SendEmailVerificationOtp_06",
                Description = "OTP_EXPIRATION_SECONDS=600 → TTL=600s used for OTP Redis key",
                ExpectedResult = "SetAsync called with TTL=600s", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "configValue=600 => TimeSpan.FromSeconds(600)" }
            });
        }
    }
}
