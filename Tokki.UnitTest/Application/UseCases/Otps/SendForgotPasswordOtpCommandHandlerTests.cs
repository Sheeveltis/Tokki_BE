using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.ForgotPassword;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class SendForgotPasswordOtpCommandHandlerTests
    {
        private static SendForgotPasswordOtpCommandHandler CreateHandler(
            Mock<IAccountRepository>? account = null,
            Mock<IRedisService>? redis = null,
            Mock<IEmailService>? email = null,
            Mock<ISystemConfigRepository>? sysConfig = null)
        {
            var mockConfig = sysConfig ?? BuildDefaultConfig("300");
            return new SendForgotPasswordOtpCommandHandler(
                (account ?? new Mock<IAccountRepository>()).Object,
                (redis   ?? new Mock<IRedisService>()).Object,
                (email   ?? new Mock<IEmailService>()).Object,
                mockConfig.Object);
        }

        private static Mock<ISystemConfigRepository> BuildDefaultConfig(string value)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS")).ReturnsAsync(value);
            return m;
        }

        private static Account ActiveUser(string email) => new()
        {
            UserId = "USER-001",
            Email  = email,
            Status = AccountStatus.Active
        };

        // TC-01: User not found → UserNotFound failure
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturnUserNotFound()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("notfound@test.com")).ReturnsAsync((Account?)null);

            var result = await CreateHandler(account: account)
                .Handle(new SendForgotPasswordOtpCommand { Email = "notfound@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Send Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "SendForgotPasswordOtp", TestCaseID = "TC-OTP-FPW-01",
                Description = "Email not registered → UserNotFound failure",
                ExpectedResult = "Return Failure UserNotFound", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "user == null => UserNotFound" }
            });
        }

        // TC-02: User is Banned → AccountBanned failure
        [Fact]
        public async Task Handle_UserBanned_ShouldReturnAccountBanned()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("banned@test.com"))
                   .ReturnsAsync(new Account { Email = "banned@test.com", Status = AccountStatus.Banned });

            var result = await CreateHandler(account: account)
                .Handle(new SendForgotPasswordOtpCommand { Email = "banned@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Send Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "SendForgotPasswordOtp", TestCaseID = "TC-OTP-FPW-02",
                Description = "User is Banned → AccountBanned failure",
                ExpectedResult = "Return Failure AccountBanned", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "user.Status == Banned => AccountBanned" }
            });
        }

        // TC-03: Valid user → OTP saved to Redis with correct key
        [Fact]
        public async Task Handle_ValidUser_ShouldSaveOtpToRedis()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("user@test.com")).ReturnsAsync(ActiveUser("user@test.com"));

            var redis = new Mock<IRedisService>();

            await CreateHandler(account: account, redis: redis)
                .Handle(new SendForgotPasswordOtpCommand { Email = "user@test.com" }, CancellationToken.None);

            redis.Verify(x => x.SetAsync(
                "OTP:ResetPassword:user@test.com",
                It.Is<string>(v => v.Contains("OtpCode")),
                It.IsAny<TimeSpan>()), Times.Once);

            QACollector.LogTestCase("OTP - Send Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "SendForgotPasswordOtp", TestCaseID = "TC-OTP-FPW-03",
                Description = "Valid user → OTP saved to Redis key 'OTP:ResetPassword:{email}'",
                ExpectedResult = "SetAsync called with correct key", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "valid user => SetAsync('OTP:ResetPassword:{email}')" }
            });
        }

        // TC-04: Valid user → email sent, return 200
        [Fact]
        public async Task Handle_ValidUser_ShouldSendEmailAndReturn200()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("user@test.com")).ReturnsAsync(ActiveUser("user@test.com"));

            var email = new Mock<IEmailService>();

            var result = await CreateHandler(account: account, email: email)
                .Handle(new SendForgotPasswordOtpCommand { Email = "user@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            email.Verify(x => x.SendEmailAsync("user@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("OTP - Send Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "SendForgotPasswordOtp", TestCaseID = "TC-OTP-FPW-04",
                Description = "Valid user → SendEmailAsync called, Return 200",
                ExpectedResult = "Return 200, SendEmailAsync once", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "valid user => SendEmailAsync => 200" }
            });
        }

        // TC-05: Custom TTL from config used
        [Fact]
        public async Task Handle_CustomLifetime_ShouldUseTtlFromConfig()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("user@test.com")).ReturnsAsync(ActiveUser("user@test.com"));

            var redis = new Mock<IRedisService>();
            var sysConfig = BuildDefaultConfig("600");

            await CreateHandler(account: account, redis: redis, sysConfig: sysConfig)
                .Handle(new SendForgotPasswordOtpCommand { Email = "user@test.com" }, CancellationToken.None);

            redis.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                TimeSpan.FromSeconds(600)), Times.Once);

            QACollector.LogTestCase("OTP - Send Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "SendForgotPasswordOtp", TestCaseID = "TC-OTP-FPW-05",
                Description = "OTP_EXPIRATION_SECONDS=600 → SetAsync with TTL=600s",
                ExpectedResult = "SetAsync(TTL=600s)", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "configValue='600' => TimeSpan.FromSeconds(600)" }
            });
        }

        // TC-06: Banned user → Redis never called
        [Fact]
        public async Task Handle_BannedUser_ShouldNotCallRedisOrEmail()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("banned@test.com"))
                   .ReturnsAsync(new Account { Email = "banned@test.com", Status = AccountStatus.Banned });

            var redis = new Mock<IRedisService>();
            var email = new Mock<IEmailService>();

            await CreateHandler(account: account, redis: redis, email: email)
                .Handle(new SendForgotPasswordOtpCommand { Email = "banned@test.com" }, CancellationToken.None);

            redis.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
            email.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            QACollector.LogTestCase("OTP - Send Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "SendForgotPasswordOtp", TestCaseID = "TC-OTP-FPW-06",
                Description = "Banned user → early return, Redis and Email never called",
                ExpectedResult = "Redis.SetAsync Times.Never, SendEmailAsync Times.Never", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Banned => early return => no side effects" }
            });
        }
    }
}
