using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps.Commands
{
    public class VerifyEmailOtpCommandHandlerTests
    {
        private readonly Mock<IRedisService> _redisServiceMock = new();
        private readonly Mock<ISystemConfigRepository> _systemConfigRepositoryMock = new();

        private VerifyEmailOtpCommandHandler CreateHandler()
        {
            return new VerifyEmailOtpCommandHandler(_redisServiceMock.Object, _systemConfigRepositoryMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // VerifyEmailOtpCommandHandler_01 | A | OTP Not Found In Redis -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OtpNotFound_ShouldReturnFailure()
        {
            // Arrange
            var command = new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "123456" };
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync("5");
            _redisServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.OtpNotFound.Code);

            // Excel Log
            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler",
                TestCaseID = "VerifyEmailOtpCommandHandler_01",
                Description = "Returns failure when OTP is not found in Redis",
                ExpectedResult = "Failure OtpNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Redis GetAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // VerifyEmailOtpCommandHandler_02 | A | OTP Invalid JSON -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InvalidJson_ShouldReturnFailure()
        {
            // Arrange
            var command = new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "123456" };
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync("5");
            _redisServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync("null");
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.OtpNotFound.Code);

            // Excel Log
            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler",
                TestCaseID = "VerifyEmailOtpCommandHandler_02",
                Description = "Returns failure when JSON is null/invalid",
                ExpectedResult = "Failure OtpNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "JsonSerializer fails to serialize proper object" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // VerifyEmailOtpCommandHandler_03 | A | OTP Max Retry Exceeded Initially -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MaxRetryExceededInitially_ShouldReturnFailure()
        {
            // Arrange
            var command = new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "123456" };
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync("3");
            var entry = JsonSerializer.Serialize(new { OtpCode = "111111", AttemptCount = 3 });
            _redisServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(entry);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.OtpMaxRetryExceeded.Code);

            // Excel Log
            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler",
                TestCaseID = "VerifyEmailOtpCommandHandler_03",
                Description = "Fails immediately if attempt count matches or exceeds max limit",
                ExpectedResult = "Failure OtpMaxRetryExceeded",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AttemptCount >= limit" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // VerifyEmailOtpCommandHandler_04 | A | Wrong OTP Code -> Attempt Increased -> Failure 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongOtp_ShouldIncreaseAttempt_AndReturn400()
        {
            // Arrange
            var command = new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "wrong" };
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync("3");
            var entry = JsonSerializer.Serialize(new { OtpCode = "123456", AttemptCount = 1 });
            _redisServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(entry);
            _redisServiceMock.Setup(x => x.GetTtlAsync(It.IsAny<string>())).ReturnsAsync(TimeSpan.FromSeconds(100));
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Bạn còn 1 lần thử"); // 3 limit - (1+1) = 1
            _redisServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler",
                TestCaseID = "VerifyEmailOtpCommandHandler_04",
                Description = "Wrong OTP increases attempt count and says how many tries left",
                ExpectedResult = "Return 400 with remaining tries message",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Wrong OTP, limit not yet reached" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // VerifyEmailOtpCommandHandler_05 | A | Wrong OTP Code Reaches Max -> Revokes -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongOtp_ReachesLimit_ShouldRevokeAndReturnFailure()
        {
            // Arrange
            var command = new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "wrong" };
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync("2");
            var entry = JsonSerializer.Serialize(new { OtpCode = "123456", AttemptCount = 1 });
            _redisServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(entry);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.OtpRevoked.Code);
            _redisServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler",
                TestCaseID = "VerifyEmailOtpCommandHandler_05",
                Description = "Wrong OTP that hits the max limit causes OTP to be revoked/deleted",
                ExpectedResult = "Failure OtpRevoked, Deleted from Redis",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Attempt reaches limit on failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // VerifyEmailOtpCommandHandler_06 | N | Correct OTP -> Deletes and Returns 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CorrectOtp_ShouldDeleteAndReturn200()
        {
            // Arrange
            var command = new VerifyEmailOtpCommand { Email = "test@test.com", OtpCode = "123456" };
            _systemConfigRepositoryMock.Setup(x => x.GetValueByKeyAsync("OTP_RETRY_LIMIT")).ReturnsAsync("3");
            var entry = JsonSerializer.Serialize(new { OtpCode = "123456", AttemptCount = 0 });
            _redisServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(entry);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            _redisServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("OTP - Verify Email", new TestCaseDetail
            {
                FunctionGroup = "VerifyEmailOtpCommandHandler",
                TestCaseID = "VerifyEmailOtpCommandHandler_06",
                Description = "Correct OTP deletes it from Redis and returns 200 success",
                ExpectedResult = "Return 200, delete from Redis",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "OTP matches" }
            });
        }
    }
}
