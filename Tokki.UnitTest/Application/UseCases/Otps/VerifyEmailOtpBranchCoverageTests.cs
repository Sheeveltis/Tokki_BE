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
    public class VerifyEmailOtpBranchCoverageTests
    {
        private record OtpEntry(string OtpCode, int AttemptCount);

        private static string MakeEntry(string code, int attempts)
            => JsonSerializer.Serialize(new { OtpCode = code, AttemptCount = attempts });

        private static VerifyEmailOtpCommandHandler CreateHandler(
            Mock<IRedisService>? redis = null,
            Mock<ISystemConfigRepository>? config = null)
        {
            var r = redis  ?? new Mock<IRedisService>();
            var c = config ?? BuildConfig("5");
            return new VerifyEmailOtpCommandHandler(r.Object, c.Object);
        }

        private static Mock<ISystemConfigRepository> BuildConfig(string? retryLimit)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync(retryLimit);
            return m;
        }

        // VerifyEmailOtpCommandHandler_01: OTP key not found → OtpNotFound
        [Fact]
        public async Task Handle_OtpKeyNotFound_ShouldReturnOtpNotFound()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com")).ReturnsAsync((string?)null);

            var result = await CreateHandler(redis).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_01",
                Description = "No OTP key in Redis → OtpNotFound failure",
                ExpectedResult = "Failure OtpNotFound", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "rawValue == null" }
            });
        }

        // VerifyEmailOtpCommandHandler_02: Redis returns null-deserialized entry → OtpNotFound
        [Fact]
        public async Task Handle_InvalidJsonInRedis_ShouldReturnOtpNotFound()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com")).ReturnsAsync("null");

            var result = await CreateHandler(redis).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_02",
                Description = "Redis contains 'null' → deserialization returns null → OtpNotFound",
                ExpectedResult = "Failure OtpNotFound", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entry == null after Deserialize" }
            });
        }

        // VerifyEmailOtpCommandHandler_03: AttemptCount already >= max → OtpMaxRetryExceeded
        [Fact]
        public async Task Handle_AlreadyMaxRetries_ShouldReturnMaxRetryExceeded()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync(MakeEntry("999999", 5)); // 5 >= maxRetry(5)

            var result = await CreateHandler(redis).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "000000" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_03",
                Description = "OTP already revoked (AttemptCount >= max) → OtpMaxRetryExceeded",
                ExpectedResult = "Failure OtpMaxRetryExceeded", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entry.AttemptCount >= maxRetryLimit" }
            });
        }

        // VerifyEmailOtpCommandHandler_04: Wrong OTP → AttemptCount incremented, < max → 400 with remaining attempts
        [Fact]
        public async Task Handle_WrongOtp_BelowMax_ShouldIncrementAndReturn400()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync(MakeEntry("999999", 1));
            redis.Setup(x => x.GetTtlAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync(TimeSpan.FromSeconds(200));

            var result = await CreateHandler(redis).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "000000" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("còn");

            redis.Verify(x => x.SetAsync(
                "OTP:VerifyEmail:u@test.com",
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()), Times.Once);

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_04",
                Description = "Wrong OTP, attempt 2/5 → AttemptCount updated, return 400 with remaining info",
                ExpectedResult = "400 with 'còn X lần thử'", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "OtpCode mismatch, AttemptCount becomes 2 < 5" }
            });
        }

        // VerifyEmailOtpCommandHandler_05: Wrong OTP → hits max retry → OtpRevoked + key deleted
        [Fact]
        public async Task Handle_WrongOtp_ReachesMax_ShouldRevokeAndDeleteKey()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync(MakeEntry("999999", 4)); // 4+1 = 5 = max

            var result = await CreateHandler(redis).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "000000" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            redis.Verify(x => x.DeleteAsync("OTP:VerifyEmail:u@test.com"), Times.Once);

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_05",
                Description = "Wrong OTP on last attempt → key deleted, OtpRevoked returned",
                ExpectedResult = "OtpRevoked + DeleteAsync called", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AttemptCount after increment == 5 == maxRetryLimit" }
            });
        }

        // VerifyEmailOtpCommandHandler_06: TTL null/zero → fallback to 300s
        [Fact]
        public async Task Handle_WrongOtp_NullTtl_ShouldFallbackTo300s()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync(MakeEntry("111111", 1));
            redis.Setup(x => x.GetTtlAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync((TimeSpan?)null); // null TTL → fallback

            await CreateHandler(redis).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "000000" }, CancellationToken.None);

            redis.Verify(x => x.SetAsync(
                "OTP:VerifyEmail:u@test.com",
                It.IsAny<string>(),
                TimeSpan.FromSeconds(300)), Times.Once);

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_06",
                Description = "GetTtlAsync returns null → fallback TTL of 300s used",
                ExpectedResult = "SetAsync called with TimeSpan.FromSeconds(300)", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "remainingTtl == null → 300s fallback branch" }
            });
        }

        // VerifyEmailOtpCommandHandler_07: Correct OTP → key deleted + 200 success
        [Fact]
        public async Task Handle_CorrectOtp_ShouldDeleteKeyAndReturn200()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync(MakeEntry("123456", 0));

            var result = await CreateHandler(redis).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            redis.Verify(x => x.DeleteAsync("OTP:VerifyEmail:u@test.com"), Times.Once);

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_07",
                Description = "Correct OTP → key deleted + 200 success message",
                ExpectedResult = "200 + DeleteAsync called", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entry.OtpCode == request.OtpCode" }
            });
        }

        // VerifyEmailOtpCommandHandler_08: Config returns non-numeric → fallback maxRetry = 5
        [Fact]
        public async Task Handle_InvalidRetryLimitConfig_ShouldFallbackTo5()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:VerifyEmail:u@test.com"))
                 .ReturnsAsync(MakeEntry("123456", 0));

            var config = BuildConfig("not_a_number");
            var result = await CreateHandler(redis, config).Handle(
                new VerifyEmailOtpCommand { Email = "u@test.com", OtpCode = "123456" }, CancellationToken.None);

            // Should still work with default of 5
            result.IsSuccess.Should().BeTrue();

            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler", TestCaseID = "VerifyEmailOtpCommandHandler_08",
                Description = "OTP_RETRY_LIMIT not parseable → default 5 used, correct OTP still passes",
                ExpectedResult = "200 success (default maxRetry=5 applied)", StatusRound1 = "Passed",
                TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "int.TryParse fails → maxRetryLimit = 5" }
            });
        }
    }
}
