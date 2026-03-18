using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class VerifyForgotPasswordOtpCommandHandlerTests
    {
        private VerifyForgotPasswordOtpCommandHandler CreateHandler(
            Mock<IOtpRepository>? otpRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IJwtTokenGenerator>? jwtGen = null)
        {
            var mockJwt = jwtGen ?? new Mock<IJwtTokenGenerator>();
            mockJwt.Setup(x => x.GenerateForgotPasswordToken(It.IsAny<string>()))
                   .Returns("fake-reset-token");

            return new VerifyForgotPasswordOtpCommandHandler(
                (otpRepo ?? MockOtpRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                mockJwt.Object);
        }

        [Fact]
        public async Task Handle_NoValidOtp_ShouldReturnFailure()
        {
            // Arrange — không có OTP hợp lệ
            var command = new VerifyForgotPasswordOtpCommand
            {
                Email = "user@tokki.com",
                OtpCode = "123456"
            };

            var mockOtpRepo = MockOtpRepository.GetMock(latestOtp: null);

            var handler = CreateHandler(otpRepo: mockOtpRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "Verify Forgot Password OTP",
                TestCaseID = "TC-OTP-VFP-01",
                Description = "Không có OTP hợp lệ (null) → return Failure OtpInvalid",
                ExpectedResult = "Return Failure OtpInvalid",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetLatestValidOtpAsync returns null",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_WrongOtpCode_ShouldReturnFailure()
        {
            // Arrange — OTP tồn tại nhưng code sai
            var command = new VerifyForgotPasswordOtpCommand
            {
                Email = "user@tokki.com",
                OtpCode = "999999" // sai
            };

            var validOtp = new Otp
            {
                OtpId = "OTP-001",
                Email = "user@tokki.com",
                OtpCode = "123456", // đúng phải là 123456
                Type = OtpType.ResetPassword,
                Status = OtpStatus.Active,
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddMinutes(5)
            };

            var mockOtpRepo = MockOtpRepository.GetMock(latestOtp: validOtp);

            var handler = CreateHandler(otpRepo: mockOtpRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "Verify Forgot Password OTP",
                TestCaseID = "TC-OTP-VFP-02",
                Description = "OTP code sai → return Failure OtpCodeWrong",
                ExpectedResult = "Return Failure OtpCodeWrong",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "OTP tồn tại nhưng OtpCode sai",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidOtp_ShouldMarkUsedAndReturnResetToken()
        {
            // Arrange — OTP hợp lệ và code đúng
            var command = new VerifyForgotPasswordOtpCommand
            {
                Email = "user@tokki.com",
                OtpCode = "123456"
            };

            var validOtp = new Otp
            {
                OtpId = "OTP-001",
                Email = "user@tokki.com",
                OtpCode = "123456",
                Type = OtpType.ResetPassword,
                Status = OtpStatus.Active,
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddMinutes(5)
            };

            var mockOtpRepo = MockOtpRepository.GetMock(latestOtp: validOtp);
            mockOtpRepo.Setup(x => x.UpdateAsync(It.IsAny<Otp>()))
                       .Returns(Task.CompletedTask);

            var handler = CreateHandler(otpRepo: mockOtpRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("fake-reset-token");

            // OTP phải được đánh dấu Used
            validOtp.Status.Should().Be(OtpStatus.Used);
            validOtp.UsedAt.Should().NotBeNull();

            mockOtpRepo.Verify(x => x.UpdateAsync(It.IsAny<Otp>()), Times.Once);

            QACollector.LogTestCase("OTP - Verify Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "Verify Forgot Password OTP",
                TestCaseID = "TC-OTP-VFP-03",
                Description = "OTP hợp lệ, code đúng → mark Used, trả về reset token",
                ExpectedResult = "Return 200, OTP.Status = Used, Data = reset token",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid OTP",
                    "OtpCode khớp",
                    "OTP.Status = Used",
                    "Return 200 với reset token"
                }
            });
        }
    }
}