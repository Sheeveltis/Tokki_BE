using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class SendGeneralOtpCommandHandlerTests
    {
        private SendGeneralOtpCommandHandler CreateHandler(
            Mock<IOtpRepository>? otpRepo = null,
            Mock<IEmailService>? emailService = null)
        {
            var mockEmail = emailService ?? new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            return new SendGeneralOtpCommandHandler(
                (otpRepo ?? MockOtpRepository.GetMock()).Object,
                mockEmail.Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_ValidEmail_ShouldCreateOtpAndSendEmailAndReturn200()
        {
            // Arrange
            var command = new SendGeneralOtpCommand
            {
                Email = "user@tokki.com"
            };

            Otp? capturedOtp = null;
            var mockOtpRepo = MockOtpRepository.GetMock();
            mockOtpRepo.Setup(x => x.AddAsync(It.IsAny<Otp>()))
                       .Callback<Otp>(o => capturedOtp = o)
                       .Returns(Task.CompletedTask);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                otpRepo: mockOtpRepo,
                emailService: mockEmail);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            capturedOtp.Should().NotBeNull();
            capturedOtp!.Email.Should().Be("user@tokki.com");
            capturedOtp.Type.Should().Be(OtpType.General);
            capturedOtp.Status.Should().Be(OtpStatus.Active);

            mockEmail.Verify(x => x.SendEmailAsync(
                "user@tokki.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("OTP - Send General OTP", new TestCaseDetail
            {
                FunctionGroup = "Send General OTP",
                TestCaseID = "TC-OTP-GEN-01",
                Description = "Gửi General OTP với email hợp lệ → tạo OTP type=General và gửi email",
                ExpectedResult = "Return 200, OTP.Type = General, email sent once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid email",
                    "OTP type = General",
                    "Email sent once",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_OtpShouldExpireIn5Minutes()
        {
            // Arrange - kiểm tra OTP expire đúng 5 phút
            var command = new SendGeneralOtpCommand
            {
                Email = "user@tokki.com"
            };

            Otp? capturedOtp = null;
            var mockOtpRepo = MockOtpRepository.GetMock();
            mockOtpRepo.Setup(x => x.AddAsync(It.IsAny<Otp>()))
                       .Callback<Otp>(o => capturedOtp = o)
                       .Returns(Task.CompletedTask);

            var handler = CreateHandler(otpRepo: mockOtpRepo);

            // Act
            var before = DateTime.UtcNow.AddHours(7).AddMinutes(4);
            var result = await handler.Handle(command, CancellationToken.None);
            var after = DateTime.UtcNow.AddHours(7).AddMinutes(6);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedOtp!.ExpiredAt.Should().BeAfter(before);
            capturedOtp.ExpiredAt.Should().BeBefore(after);

            QACollector.LogTestCase("OTP - Send General OTP", new TestCaseDetail
            {
                FunctionGroup = "Send General OTP",
                TestCaseID = "TC-OTP-GEN-02",
                Description = "OTP được tạo với ExpiredAt = now + 5 phút (boundary: thời gian hết hạn)",
                ExpectedResult = "OTP.ExpiredAt ≈ now + 5 minutes",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "OTP.ExpiredAt = CreatedAt + 5 minutes (boundary)",
                    "Kiểm tra thời gian hết hạn"
                }
            });
        }

        [Fact]
        public async Task Handle_EmailServiceThrows_ShouldPropagateException()
        {
            // Arrange — handler không có try/catch → exception propagate
            var command = new SendGeneralOtpCommand
            {
                Email = "user@tokki.com"
            };

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                     .ThrowsAsync(new Exception("Email service down"));

            var handler = CreateHandler(emailService: mockEmail);

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert — ⚠️ Test này có thể FAIL nếu handler có try/catch
            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("Email service down");

            QACollector.LogTestCase("OTP - Send General OTP", new TestCaseDetail
            {
                FunctionGroup = "Send General OTP",
                TestCaseID = "TC-OTP-GEN-03",
                Description = "Email service throw exception → exception propagate (handler không có try/catch)",
                ExpectedResult = "Exception propagates với message 'Email service down'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "SendEmailAsync throws Exception",
                    "Handler không có try/catch",
                    "Exception propagates"
                }
            });
        }
    }
}