using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class SendGeneralOtpCommandHandlerTests
    {
        private static SendGeneralOtpCommandHandler CreateHandler(
            Mock<IRedisService>? redis = null,
            Mock<IEmailService>? email = null,
            Mock<IAccountRepository>? account = null)
        {
            return new SendGeneralOtpCommandHandler(
                (redis   ?? new Mock<IRedisService>()).Object,
                (email   ?? new Mock<IEmailService>()).Object,
                (account ?? new Mock<IAccountRepository>()).Object);
        }

        private static Account ActiveUser(string email) => new()
        {
            UserId = "USER-001",
            Email  = email,
            Status = AccountStatus.Active
        };

        // TC-01: User not found → failure (no status code specified in handler)
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturnFailure()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("notfound@test.com")).ReturnsAsync((Account?)null);

            var result = await CreateHandler(account: account)
                .Handle(new SendGeneralOtpCommand { Email = "notfound@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtp", TestCaseID = "TC-OTP-GEN-01",
                Description = "Email not registered → UserNotFound failure",
                ExpectedResult = "Return Failure (UserNotFound)", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByEmailAsync returns null => failure" }
            });
        }

        // TC-02: User found → OTP saved to Redis with correct key prefix
        [Fact]
        public async Task Handle_ValidEmail_ShouldSaveOtpToRedis()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("user@test.com")).ReturnsAsync(ActiveUser("user@test.com"));

            var redis = new Mock<IRedisService>();
            var email = new Mock<IEmailService>();

            await CreateHandler(redis: redis, email: email, account: account)
                .Handle(new SendGeneralOtpCommand { Email = "user@test.com" }, CancellationToken.None);

            redis.Verify(x => x.SetAsync(
                "OTP:General:user@test.com",
                It.Is<string>(v => v.Contains("OtpCode")),
                TimeSpan.FromMinutes(5)),
                Times.Once);

            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtp", TestCaseID = "TC-OTP-GEN-02",
                Description = "Valid email → Redis.SetAsync called with key 'OTP:General:{email}'",
                ExpectedResult = "SetAsync called with correct key and 5 min TTL", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "user found => SetAsync(key, value, 5min)" }
            });
        }

        // TC-03: Valid email → email is sent
        [Fact]
        public async Task Handle_ValidEmail_ShouldSendEmail()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("user@test.com")).ReturnsAsync(ActiveUser("user@test.com"));

            var redis = new Mock<IRedisService>();
            var email = new Mock<IEmailService>();

            await CreateHandler(redis: redis, email: email, account: account)
                .Handle(new SendGeneralOtpCommand { Email = "user@test.com" }, CancellationToken.None);

            email.Verify(x => x.SendEmailAsync("user@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtp", TestCaseID = "TC-OTP-GEN-03",
                Description = "Valid email → SendEmailAsync called once",
                ExpectedResult = "SendEmailAsync called with correct email", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "user found => SendEmailAsync once" }
            });
        }

        // TC-04: Valid email → OTP value has OtpCode and AttemptCount=0
        [Fact]
        public async Task Handle_ValidEmail_ShouldStoreOtpWithZeroAttempt()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("user@test.com")).ReturnsAsync(ActiveUser("user@test.com"));

            string? capturedValue = null;
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                 .Callback<string, string, TimeSpan>((_, v, _) => capturedValue = v)
                 .Returns(Task.CompletedTask);

            await CreateHandler(redis: redis, account: account)
                .Handle(new SendGeneralOtpCommand { Email = "user@test.com" }, CancellationToken.None);

            capturedValue.Should().NotBeNull();
            var obj = JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(capturedValue!);
            obj!["AttemptCount"]!.GetValue<int>().Should().Be(0);
            obj["OtpCode"]!.ToString().Should().HaveLength(6);

            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtp", TestCaseID = "TC-OTP-GEN-04",
                Description = "Redis value has OtpCode (6 digits) and AttemptCount=0",
                ExpectedResult = "AttemptCount=0, OtpCode.Length=6", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Serialized OTP JSON structure validated" }
            });
        }

        // TC-05: Valid email → Return 200 success
        [Fact]
        public async Task Handle_ValidEmail_ShouldReturn200()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync("user@test.com")).ReturnsAsync(ActiveUser("user@test.com"));

            var result = await CreateHandler(account: account)
                .Handle(new SendGeneralOtpCommand { Email = "user@test.com" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtp", TestCaseID = "TC-OTP-GEN-05",
                Description = "Valid email → Return 200 Success",
                ExpectedResult = "Return 200", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All steps OK => 200 success" }
            });
        }

        // TC-06: User not found → Redis.SetAsync never called
        [Fact]
        public async Task Handle_UserNotFound_ShouldNotCallRedis()
        {
            var account = new Mock<IAccountRepository>();
            account.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);

            var redis = new Mock<IRedisService>();

            await CreateHandler(redis: redis, account: account)
                .Handle(new SendGeneralOtpCommand { Email = "ghost@test.com" }, CancellationToken.None);

            redis.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);

            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtp", TestCaseID = "TC-OTP-GEN-06",
                Description = "User not found → Redis.SetAsync never called",
                ExpectedResult = "SetAsync Times.Never", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "user not found => early return, no Redis call" }
            });
        }
    }
}
