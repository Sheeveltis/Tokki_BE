using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Application.UseCases.Otps.Commands
{
    public class SendGeneralOtpCommandHandlerTests
    {
        private readonly Mock<IRedisService> _redisServiceMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();

        private SendGeneralOtpCommandHandler CreateHandler()
        {
            return new SendGeneralOtpCommandHandler(
                _redisServiceMock.Object,
                _emailServiceMock.Object,
                _accountRepositoryMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-OTP-SG-01 | A | User Not Found -> Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturnFailure()
        {
            // Arrange
            var command = new SendGeneralOtpCommand { Email = "notfound@test.com" };
            _accountRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync((Account?)null);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.UserNotFound.Code);

            // Excel Log
            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtpCommandHandler",
                TestCaseID = "TC-OTP-SG-01",
                Description = "Returns failure when user email is not found",
                ExpectedResult = "Return failure with UserNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Account is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-OTP-SG-02 | N | Success -> Sets Redis and Sends Email -> 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var command = new SendGeneralOtpCommand { Email = "found@test.com" };
            _accountRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync(new Account { Email = command.Email });
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            _redisServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtpCommandHandler",
                TestCaseID = "TC-OTP-SG-02",
                Description = "Successfully creates OTP, stores in Redis, and sends Email",
                ExpectedResult = "Return 200 Success",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User exists", "Valid request" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-OTP-SG-03 | E | Exception during Redis Set -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RedisException_ShouldThrow()
        {
            // Arrange
            var command = new SendGeneralOtpCommand { Email = "found@test.com" };
            _accountRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync(new Account { Email = command.Email });
            _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ThrowsAsync(new Exception("Redis down"));

            var handler = CreateHandler();

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Redis down");

            // Excel Log
            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtpCommandHandler",
                TestCaseID = "TC-OTP-SG-03",
                Description = "Throws exception if Redis fails",
                ExpectedResult = "Throws Exception",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Redis fails" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-OTP-SG-04 | E | Exception during Email Send -> Throws
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmailException_ShouldThrow()
        {
            // Arrange
            var command = new SendGeneralOtpCommand { Email = "found@test.com" };
            _accountRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync(new Account { Email = command.Email });
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP down"));

            var handler = CreateHandler();

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("SMTP down");

            // Excel Log
            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtpCommandHandler",
                TestCaseID = "TC-OTP-SG-04",
                Description = "Throws exception if Email sending fails",
                ExpectedResult = "Throws Exception",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SMTP fails" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-OTP-SG-05 | B | Boundary Email Null -> Assuming Validator Catches But Directly Call -> Account NotFound
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyEmail_ShouldReturnUserNotFound()
        {
            // Arrange
            var command = new SendGeneralOtpCommand { Email = "" };
            _accountRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync((Account?)null);
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.UserNotFound.Code);

            // Excel Log
            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtpCommandHandler",
                TestCaseID = "TC-OTP-SG-05",
                Description = "Passing empty email directly fails at user search",
                ExpectedResult = "Failure UserNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty email string" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-OTP-SG-06 | N | Success Content Verification
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VerifyEmailContent_ShouldContainOtpText()
        {
            // Arrange
            var command = new SendGeneralOtpCommand { Email = "found@test.com" };
            _accountRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync(new Account { Email = command.Email });
            var handler = CreateHandler();

            string sentBody = "";
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((email, sub, body) => { sentBody = body; })
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            sentBody.Should().Contain("Mã xác thực của bạn là:");

            // Excel Log
            QACollector.LogTestCase("OTP - Send General", new TestCaseDetail
            {
                FunctionGroup = "SendGeneralOtpCommandHandler",
                TestCaseID = "TC-OTP-SG-06",
                Description = "Verifies the email body contains appropriate OTP text",
                ExpectedResult = "Body should contain specific string",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid OTP Body verification" }
            });
        }
    }
}
