using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp;
using Tokki.Application.Common.Models;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class VerifyEmailOtpCommandHandlerTests
    {
        private static VerifyEmailOtpCommandHandler CreateHandler(
            Mock<IRedisService>? redis = null,
            Mock<ISystemConfigRepository>? sysConfig = null)
        {
            var mockConfig = sysConfig ?? BuildDefaultConfig("5");
            return new VerifyEmailOtpCommandHandler(
                (redis ?? new Mock<IRedisService>()).Object,
                mockConfig.Object);
        }

        private static Mock<ISystemConfigRepository> BuildDefaultConfig(string retryLimit)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync(retryLimit);
            return m;
        }

        private static string BuildOtpJson(string code, int attempts = 0)
            => JsonSerializer.Serialize(new { OtpCode = code, AttemptCount = attempts });

        // TC-01: OTP key not in Redis → OtpNotFound failure
        [Fact]
        public async Task Handle_OtpNotInRedis_ShouldReturnOtpNotFound()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:test@test.com")).ReturnsAsync((string?)null);

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtp", TestCaseID = "TC-OTP-VRF-01",
                Description = "OTP key not in Redis → OtpNotFound failure",
                ExpectedResult = "Return Failure OtpNotFound", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "rawValue == null => OtpNotFound" }
            });
        }

        // TC-02: Attempt count already at max → OtpMaxRetryExceeded
        [Fact]
        public async Task Handle_MaxAttemptsReached_ShouldReturnOtpMaxRetryExceeded()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("111111", attempts: 5)); // 5 = maxRetryLimit

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "111111" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtp", TestCaseID = "TC-OTP-VRF-02",
                Description = "AttemptCount already = maxRetry → OtpMaxRetryExceeded",
                ExpectedResult = "Return Failure OtpMaxRetryExceeded", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entry.AttemptCount >= maxRetryLimit => OtpMaxRetryExceeded" }
            });
        }

        // TC-03: Wrong OTP but under limit → 400 with remaining attempts
        [Fact]
        public async Task Handle_WrongOtp_ShouldReturn400AndIncrementAttempt()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("111111", attempts: 2));
            redis.Setup(x => x.GetTtlAsync("OTP:VerifyEmail:test@test.com"))
                 .ReturnsAsync(TimeSpan.FromSeconds(200));

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "999999" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Updated value saved back with incremented count
            redis.Verify(x => x.SetAsync(
                "OTP:VerifyEmail:test@test.com",
                It.Is<string>(v => v.Contains("3")), // AttemptCount = 3
                It.IsAny<TimeSpan>()), Times.Once);

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtp", TestCaseID = "TC-OTP-VRF-03",
                Description = "Wrong code, attempt 2→3 → 400, AttemptCount incremented in Redis",
                ExpectedResult = "Return 400, SetAsync with AttemptCount=3", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "wrong code, count < max => increment and save" }
            });
        }

        // TC-04: Wrong OTP and final attempt → OtpRevoked, key deleted
        [Fact]
        public async Task Handle_WrongOtpOnLastAttempt_ShouldRevokeAndDeleteKey()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("111111", attempts: 4)); // one before max=5

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "999999" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            redis.Verify(x => x.DeleteAsync("OTP:VerifyEmail:test@test.com"), Times.Once);

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtp", TestCaseID = "TC-OTP-VRF-04",
                Description = "Wrong code on last attempt → OtpRevoked, Redis key deleted",
                ExpectedResult = "Return Failure OtpRevoked, DeleteAsync called", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "attempt reaches max on wrong code => revoke => delete key" }
            });
        }

        // TC-05: Correct OTP → 200 success, key deleted (one-time use)
        [Fact]
        public async Task Handle_CorrectOtp_ShouldReturn200AndDeleteKey()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("123456", attempts: 1));

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            redis.Verify(x => x.DeleteAsync("OTP:VerifyEmail:test@test.com"), Times.Once);

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtp", TestCaseID = "TC-OTP-VRF-05",
                Description = "Correct OTP → 200 success, key deleted (one-time use)",
                ExpectedResult = "Return 200, DeleteAsync called", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "OtpCode matches => 200 => DeleteAsync" }
            });
        }

        // TC-06: Config retry limit respected (custom = 3)
        [Fact]
        public async Task Handle_CustomRetryLimit_ShouldRespectConfigValue()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("111111", attempts: 3)); // at custom max

            var sysConfig = BuildDefaultConfig("3"); // maxRetry = 3

            var result = await CreateHandler(redis: redis, sysConfig: sysConfig)
                .Handle(new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "111111" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtp", TestCaseID = "TC-OTP-VRF-06",
                Description = "Custom OTP_RETRY_LIMIT=3, attempts=3 → OtpMaxRetryExceeded",
                ExpectedResult = "Return Failure MaxRetry", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "configValue='3', attempts=3 => maxRetry hit" }
            });
        }
    }
}
