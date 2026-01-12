using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Otps.Commands
{
    public class SendGeneralOtpCommandHandlerTests : OtpTestBase
    {
        private readonly SendGeneralOtpCommandHandler _handler;

        public SendGeneralOtpCommandHandlerTests()
        {
            _handler = new SendGeneralOtpCommandHandler(
                _mockOtpRepo.Object,
                _mockEmailService.Object,
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
        public async Task Handle_Should_ReturnSuccess_When_RequestIsValid()
        {
            // Arrange
            var command = OtpTestData.GetValidGeneralOtpCommand();
            var expectedOtpId = "otp-gen-1234567"; // bạn đang giả lập 15 ký tự

            _mockIdGenerator.Setup(x => x.Generate(15))
                            .Returns(expectedOtpId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Success("Đã gửi OTP thành công.", 200) => chuỗi thường nằm ở Data
            result.Data.Should().Be("Đã gửi OTP thành công.");

            _mockOtpRepo.Verify(x => x.AddAsync(It.Is<Otp>(o =>
                o.OtpId == expectedOtpId &&
                o.Email == command.Email &&
                o.Type == OtpType.General &&
                o.UserId == null &&
                o.Status == OtpStatus.Active &&
                !string.IsNullOrEmpty(o.OtpCode) &&
                o.OtpCode.Length == 6
            )), Times.Once);

            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Subject trong handler: "Mã xác thực " (có space cuối)
            _mockEmailService.Verify(x => x.SendEmailAsync(
                command.Email,
                "Mã xác thực ",
                It.Is<string>(b => b.Contains("Mã xác thực của bạn là"))
            ), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_DatabaseFails()
        {
            // Arrange
            var command = OtpTestData.GetValidGeneralOtpCommand();

            _mockOtpRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("DB Connection Lost"));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("DB Connection Lost");

            _mockEmailService.Verify(
                x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never
            );
        }
    }
}
