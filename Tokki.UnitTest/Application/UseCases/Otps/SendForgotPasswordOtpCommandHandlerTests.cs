using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.ForgotPassword;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class SendForgotPasswordOtpCommandHandlerTests
    {
        private SendForgotPasswordOtpCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IOtpRepository>? otpRepo = null,
            Mock<IEmailService>? emailService = null,
            Mock<ISystemConfigRepository>? configRepo = null)
        {
            return new SendForgotPasswordOtpCommandHandler(
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (otpRepo ?? MockOtpRepository.GetMock()).Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                (configRepo ?? MockSystemConfigRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_UserNotFound_ShouldReturnFailure()
        {
            var command = new SendForgotPasswordOtpCommand { Email = "notfound@tokki.com" };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((Account?)null);

            var handler = CreateHandler(accountRepo: mockAccountRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "Send Forgot Password OTP",
                TestCaseID = "TC-OTP-FPW-01",
                Description = "Gửi OTP quên mật khẩu với email không tồn tại",
                ExpectedResult = "Return Failure UserNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email không tồn tại trong DB",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_UserBanned_ShouldReturnFailure()
        {
            var command = new SendForgotPasswordOtpCommand { Email = "banned@tokki.com" };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync(new Account
                           {
                               Email = "banned@tokki.com",
                               Status = AccountStatus.Banned
                           });

            var handler = CreateHandler(accountRepo: mockAccountRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("OTP - Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "Send Forgot Password OTP",
                TestCaseID = "TC-OTP-FPW-02",
                Description = "Gửi OTP cho tài khoản bị banned",
                ExpectedResult = "Return Failure AccountBanned",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.Status = Banned",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidUser_ShouldSendOtpEmailAndReturnSuccess()
        {
            var command = new SendForgotPasswordOtpCommand { Email = "user@tokki.com" };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync(new Account
                           {
                               Email = "user@tokki.com",
                               Status = AccountStatus.Active
                           });

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                accountRepo: mockAccountRepo,
                emailService: mockEmail);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            mockEmail.Verify(x => x.SendEmailAsync(
                "user@tokki.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("OTP - Forgot Password", new TestCaseDetail
            {
                FunctionGroup = "Send Forgot Password OTP",
                TestCaseID = "TC-OTP-FPW-03",
                Description = "Gửi OTP quên mật khẩu hợp lệ → tạo OTP và gửi email",
                ExpectedResult = "Return 200, email sent once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid email, account Active",
                    "OTP created",
                    "Email sent once",
                    "Return 200"
                }
            });
        }
    }
}