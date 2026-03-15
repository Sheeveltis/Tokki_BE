using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Otps
{
    public class SendEmailVerificationOtpCommandHandlerTests
    {
        private SendGeneralOtpCommandHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IOtpRepository>? otpRepo = null,
            Mock<IEmailService>? emailService = null,
            Mock<ISystemConfigRepository>? configRepo = null)
        {
            return new SendGeneralOtpCommandHandler(
                (otpRepo ?? MockOtpRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                new Mock<IValidator<SendEmailVerificationOtpCommand>>().Object,
                (configRepo ?? MockSystemConfigRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_EmailAlreadyExists_ActiveAccount_ShouldReturn400()
        {
            var command = new SendEmailVerificationOtpCommand
            {
                Email = "existing@tokki.com"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync(new Account
                           {
                               Email = "existing@tokki.com",
                               Status = AccountStatus.Active
                           });

            var handler = CreateHandler(accountRepo: mockAccountRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("OTP - Email Verification", new TestCaseDetail
            {
                FunctionGroup = "Send Email Verification OTP",
                TestCaseID = "TC-OTP-EVR-01",
                Description = "Email đã đăng ký và đang Active → return 400 EmailAlreadyExists",
                ExpectedResult = "Return 400 EmailAlreadyExists",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email đã tồn tại",
                    "Account.Status = Active",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_EmailBannedAccount_ShouldReturn400()
        {
            var command = new SendEmailVerificationOtpCommand
            {
                Email = "banned@tokki.com"
            };

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
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("OTP - Email Verification", new TestCaseDetail
            {
                FunctionGroup = "Send Email Verification OTP",
                TestCaseID = "TC-OTP-EVR-02",
                Description = "Email thuộc tài khoản bị Banned/Inactive → return 400 AccountUnavailable",
                ExpectedResult = "Return 400 AccountUnavailable",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Account.Status = Banned",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_OtpRateLimit_ShouldReturn429()
        {
            var command = new SendEmailVerificationOtpCommand
            {
                Email = "new@tokki.com"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((Account?)null); // email chưa tồn tại

            // OTP vừa gửi 10 giây trước → rate limit (< 60 giây)
            var mockOtpRepo = MockOtpRepository.GetMock(
                latestOtp: MockOtpRepository.GetRecentOtp("new@tokki.com"));

            var handler = CreateHandler(
                accountRepo: mockAccountRepo,
                otpRepo: mockOtpRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(429);

            QACollector.LogTestCase("OTP - Email Verification", new TestCaseDetail
            {
                FunctionGroup = "Send Email Verification OTP",
                TestCaseID = "TC-OTP-EVR-03",
                Description = "OTP vừa được gửi 10 giây trước (< 60s rate limit) → return 429",
                ExpectedResult = "Return 429 TooManyRequests",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "OTP gửi 10 giây trước (boundary: < 60s)",
                    "Rate limit triggered",
                    "Return 429"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidNewEmail_ShouldSendOtpAndReturn200()
        {
            var command = new SendEmailVerificationOtpCommand
            {
                Email = "newuser@tokki.com"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((Account?)null); // email chưa tồn tại

            var mockOtpRepo = MockOtpRepository.GetMock(latestOtp: null); // không có OTP cũ

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                accountRepo: mockAccountRepo,
                otpRepo: mockOtpRepo,
                emailService: mockEmail);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            mockEmail.Verify(x => x.SendEmailAsync(
                "newuser@tokki.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("OTP - Email Verification", new TestCaseDetail
            {
                FunctionGroup = "Send Email Verification OTP",
                TestCaseID = "TC-OTP-EVR-04",
                Description = "Email mới chưa đăng ký, không có rate limit → gửi OTP thành công",
                ExpectedResult = "Return 200, email sent once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Email chưa tồn tại",
                    "Không có OTP cũ (no rate limit)",
                    "Email sent once",
                    "Return 200"
                }
            });
        }
    }
}