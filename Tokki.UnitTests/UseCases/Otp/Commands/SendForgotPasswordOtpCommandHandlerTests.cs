using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Otps.Commands.ForgotPassword;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Otps.Commands
{
    public class SendForgotPasswordOtpCommandHandlerTests : OtpTestBase
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly SendForgotPasswordOtpCommandHandler _handler;

        public SendForgotPasswordOtpCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();

            _handler = new SendForgotPasswordOtpCommandHandler(
                _mockAccountRepo.Object,
                _mockOtpRepo.Object,
                _mockEmailService.Object,
                _mockSystemConfigRepo.Object,
                _mockIdGenerator.Object
            );

            // Tránh await null nếu base chưa setup
            _mockOtpRepo.Setup(x => x.AddAsync(It.IsAny<Otp>()))
                        .Returns(Task.CompletedTask);

            _mockOtpRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var command = OtpTestData.GetValidForgotPasswordCommand();

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync((Account?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserNotFound.Code);

            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserIsBanned()
        {
            // Arrange
            var command = OtpTestData.GetValidForgotPasswordCommand();
            var bannedUser = OtpTestData.GetBannedAccount();

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(bannedUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountBanned.Code);

            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_ValidRequest()
        {
            // Arrange
            var command = OtpTestData.GetValidForgotPasswordCommand();
            var activeUser = OtpTestData.GetActiveAccount();
            var generatedOtpId = "otp-nano-id-123";

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(activeUser);

            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS"))
                                 .ReturnsAsync("300");

            _mockIdGenerator.Setup(x => x.Generate(15))
                            .Returns(generatedOtpId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Với style Success("...", 200) => string thường nằm ở Data
            result.Data.Should().Be("Mã OTP đã được gửi.");

            _mockOtpRepo.Verify(x => x.AddAsync(It.Is<Otp>(o =>
                o.OtpId == generatedOtpId &&
                o.Email == command.Email &&
                o.Type == OtpType.ResetPassword &&
                o.Status == OtpStatus.Active &&
                !string.IsNullOrEmpty(o.OtpCode) &&
                o.OtpCode.Length == 6
            )), Times.Once);

            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                command.Email,
                "Mã khôi phục mật khẩu",
                It.Is<string>(s => s.Contains("Mã OTP của bạn là"))
            ), Times.Once);
        }
    }
}
