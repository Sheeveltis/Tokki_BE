using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Otps.Commands
{
    public class SendForgotPasswordOtpCommandHandlerTests : OtpTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // 1. Arrange
            var command = OtpTestData.GetValidForgotPasswordCommand();

            // Giả lập: Không tìm thấy user theo email
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync((Account?)null);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserNotFound.Code);

            // Đảm bảo không gửi mail hay lưu OTP
            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserIsBanned()
        {
            // 1. Arrange
            var command = OtpTestData.GetValidForgotPasswordCommand();
            var bannedUser = OtpTestData.GetBannedAccount();

            // Giả lập: Tìm thấy user nhưng bị BAN
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(bannedUser);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountBanned.Code);

            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_ValidRequest()
        {
            // 1. Arrange
            var command = OtpTestData.GetValidForgotPasswordCommand();
            var activeUser = OtpTestData.GetActiveAccount();
            string generatedOtpId = "otp-nano-id-123";

            // Setup các Mock
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(activeUser);

            // Giả lập config thời gian hết hạn (ví dụ: 300 giây)
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS"))
                                 .ReturnsAsync("300");

            // Giả lập sinh ID
            _mockIdGenerator.Setup(x => x.Generate(15))
                            .Returns(generatedOtpId);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Mã OTP đã được gửi.");

            // Verify: OTP được thêm vào DB với thông tin đúng
            _mockOtpRepo.Verify(x => x.AddAsync(It.Is<Otp>(o =>
                o.OtpId == generatedOtpId &&
                o.Email == command.Email &&
                o.Type == OtpType.ResetPassword &&
                o.Status == OtpStatus.Active &&
                o.OtpCode.Length == 6 // Kiểm tra mã có 6 số
            )), Times.Once);

            // Verify: Đã lưu xuống DB
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify: Đã gửi email
            _mockEmailService.Verify(x => x.SendEmailAsync(
                command.Email,
                "Mã khôi phục mật khẩu",
                It.Is<string>(s => s.Contains("Mã OTP của bạn là"))
            ), Times.Once);
        }
    }
}