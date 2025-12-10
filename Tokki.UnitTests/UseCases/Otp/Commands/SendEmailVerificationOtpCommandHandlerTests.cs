using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp; // Lưu ý namespace
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Otps.Commands
{
    public class SendEmailVerificationOtpCommandHandlerTests : OtpTestBase
    {
        // Vì class bạn copy paste tên là SendGeneralOtpCommandHandler nhưng logic là VerifyEmail
        // Tôi sẽ dùng tên class theo logic đúng: SendEmailVerificationOtpCommandHandler
        private readonly SendGeneralOtpCommandHandler _handler;
        private readonly Mock<IValidator<SendEmailVerificationOtpCommand>> _mockValidator;

        public SendEmailVerificationOtpCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<SendEmailVerificationOtpCommand>>();

            // Khởi tạo Handler với đầy đủ dependency từ Base và Mock mới
            _handler = new SendGeneralOtpCommandHandler(
                _mockOtpRepo.Object,
                _mockEmailService.Object,
                _mockValidator.Object,
                _mockSystemConfigRepo.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RateLimitExceeded()
        {
            // 1. Arrange
            var command = OtpTestData.GetEmailVerificationCommand();
            var recentOtp = OtpTestData.GetRecentOtp(command.Email);

            // Giả lập: Đã có 1 OTP vừa gửi cách đây 10s
            _mockOtpRepo.Setup(x => x.GetLatestValidOtpAsync(command.Email, OtpType.VerifyEmail))
                        .ReturnsAsync(recentOtp);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(429); // Too Many Requests
            // 60s - 10s = 50s. Kiểm tra message có chứa số giây phải đợi
            result.Message.Should().Contain("Vui lòng đợi");

            // Verify: KHÔNG được lưu OTP mới, KHÔNG được gửi mail
            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_With_CustomExpirationTime()
        {
            // 1. Arrange
            var command = OtpTestData.GetEmailVerificationCommand();
            string expectedOtpId = "otp-verify-001";

            // Giả lập: Chưa có OTP nào (hoặc đã cũ) -> Rate limit pass
            _mockOtpRepo.Setup(x => x.GetLatestValidOtpAsync(command.Email, OtpType.VerifyEmail))
                        .ReturnsAsync((Otp?)null);

            // Giả lập Config: Thời gian hết hạn là 600 giây (10 phút)
            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS"))
                                 .ReturnsAsync("600");

            _mockIdGenerator.Setup(x => x.Generate(15)).Returns(expectedOtpId);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify: OTP được tạo với thời gian hết hạn ~600s
            _mockOtpRepo.Verify(x => x.AddAsync(It.Is<Otp>(o =>
                o.OtpId == expectedOtpId &&
                o.Type == OtpType.VerifyEmail &&
                (o.ExpiredAt - o.CreatedAt).TotalSeconds >= 599 // Chấp nhận sai số nhỏ
            )), Times.Once);

            _mockEmailService.Verify(x => x.SendEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_EmailServiceThrowsError()
        {
            // 1. Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            // Rate limit pass
            _mockOtpRepo.Setup(x => x.GetLatestValidOtpAsync(It.IsAny<string>(), It.IsAny<OtpType>()))
                        .ReturnsAsync((Otp?)null);

            // Giả lập: Gửi mail bị lỗi
            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Error"));

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            // Code của bạn có try-catch bọc quanh EmailService và return AppErrors.EmailServiceError
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailServiceError.Code);

            // Verify: Dù gửi mail lỗi, nhưng code của bạn vẫn đã gọi SaveChanges trước đó
            // (Đây là logic trong code của bạn: Lưu OTP -> Gửi Mail -> Nếu lỗi thì return Failure)
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}