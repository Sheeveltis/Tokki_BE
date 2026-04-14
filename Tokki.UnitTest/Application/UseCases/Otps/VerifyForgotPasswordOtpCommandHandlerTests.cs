using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class VerifyForgotPasswordOtpCommandHandlerTests
    {
        private static VerifyForgotPasswordOtpCommandHandler CreateHandler(
            Mock<IRedisService>? redis = null,
            Mock<IAccountRepository>? account = null,
            Mock<IJwtTokenGenerator>? jwtGen = null)
        {
            var mockJwt = jwtGen ?? new Mock<IJwtTokenGenerator>();
            mockJwt.Setup(x => x.GenerateForgotPasswordToken(It.IsAny<string>())).Returns("RESET-TOKEN-XYZ");

            return new VerifyForgotPasswordOtpCommandHandler(
                (redis   ?? new Mock<IRedisService>()).Object,
                (account ?? new Mock<IAccountRepository>()).Object,
                mockJwt.Object);
        }

        private static string BuildOtpJson(string code, int attempts = 0)
            => JsonSerializer.Serialize(new { OtpCode = code, AttemptCount = attempts });

        // TC-01: OTP key not in Redis → OtpInvalid
        [Fact]
        public async Task Handle_OtpKeyMissing_ShouldReturnOtpInvalid()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:ResetPassword:test@test.com")).ReturnsAsync((string?)null);

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyForgotPasswordOtpCommand { Email = "test@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "VerifyForgotPasswordOtp", TestCaseID = "TC-OTP-VFP-01",
                Description = "OTP key not in Redis → OtpInvalid failure",
                ExpectedResult = "Return Failure OtpInvalid", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "rawValue == null => OtpInvalid" }
            });
        }

        // TC-02: Deserialized entry is null → OtpInvalid
        [Fact]
        public async Task Handle_MalformedRedisValue_ShouldReturnOtpInvalid()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:ResetPassword:test@test.com")).ReturnsAsync("null");

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyForgotPasswordOtpCommand { Email = "test@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "VerifyForgotPasswordOtp", TestCaseID = "TC-OTP-VFP-02",
                Description = "Redis returns 'null' string → deserialized entry is null → OtpInvalid",
                ExpectedResult = "Return Failure OtpInvalid", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "JsonDeserialize returns null => OtpInvalid" }
            });
        }

        // TC-03: Wrong OTP code → OtpCodeWrong
        [Fact]
        public async Task Handle_WrongOtpCode_ShouldReturnOtpCodeWrong()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:ResetPassword:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("111111"));

            var result = await CreateHandler(redis: redis)
                .Handle(new VerifyForgotPasswordOtpCommand { Email = "test@test.com", OtpCode = "999999" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "VerifyForgotPasswordOtp", TestCaseID = "TC-OTP-VFP-03",
                Description = "Wrong OTP code → OtpCodeWrong failure",
                ExpectedResult = "Return Failure OtpCodeWrong", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entry.OtpCode != request.OtpCode => OtpCodeWrong" }
            });
        }

        // TC-04: Correct OTP → Redis key deleted (one-time use)
        [Fact]
        public async Task Handle_CorrectOtp_ShouldDeleteRedisKey()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:ResetPassword:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("123456"));

            await CreateHandler(redis: redis)
                .Handle(new VerifyForgotPasswordOtpCommand { Email = "test@test.com", OtpCode = "123456" }, CancellationToken.None);

            redis.Verify(x => x.DeleteAsync("OTP:ResetPassword:test@test.com"), Times.Once);

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "VerifyForgotPasswordOtp", TestCaseID = "TC-OTP-VFP-04",
                Description = "Correct OTP → Redis key deleted (one-time use)",
                ExpectedResult = "DeleteAsync called once", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "OtpCode matches => DeleteAsync" }
            });
        }

        // TC-05: Correct OTP → JWT reset token returned, 200
        [Fact]
        public async Task Handle_CorrectOtp_ShouldReturnResetToken()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:ResetPassword:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("123456"));

            var jwtGen = new Mock<IJwtTokenGenerator>();
            jwtGen.Setup(x => x.GenerateForgotPasswordToken("test@test.com")).Returns("RESET-TOKEN-ABC");

            var result = await CreateHandler(redis: redis, jwtGen: jwtGen)
                .Handle(new VerifyForgotPasswordOtpCommand { Email = "test@test.com", OtpCode = "123456" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("RESET-TOKEN-ABC");

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "VerifyForgotPasswordOtp", TestCaseID = "TC-OTP-VFP-05",
                Description = "Correct OTP → JWT reset token issued, Return 200 with token",
                ExpectedResult = "Return 200, Data='RESET-TOKEN-ABC'", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "OtpCode matches => GenerateForgotPasswordToken => 200" }
            });
        }

        // TC-06: Wrong code → DeleteAsync never called
        [Fact]
        public async Task Handle_WrongCode_ShouldNotDeleteKey()
        {
            var redis = new Mock<IRedisService>();
            redis.Setup(x => x.GetAsync("OTP:ResetPassword:test@test.com"))
                 .ReturnsAsync(BuildOtpJson("111111"));

            await CreateHandler(redis: redis)
                .Handle(new VerifyForgotPasswordOtpCommand { Email = "test@test.com", OtpCode = "999999" }, CancellationToken.None);

            redis.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "VerifyForgotPasswordOtp", TestCaseID = "TC-OTP-VFP-06",
                Description = "Wrong OTP → DeleteAsync never called",
                ExpectedResult = "DeleteAsync Times.Never", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "wrong code => no key deletion" }
            });
        }
    }
}
