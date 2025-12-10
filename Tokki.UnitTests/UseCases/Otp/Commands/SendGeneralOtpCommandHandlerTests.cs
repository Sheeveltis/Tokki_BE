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
            // Khởi tạo Handler riêng cho feature này, sử dụng các Mock từ Base class
            _handler = new SendGeneralOtpCommandHandler(
                _mockOtpRepo.Object,
                _mockEmailService.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_RequestIsValid()
        {
            // 1. Arrange
            var command = OtpTestData.GetValidGeneralOtpCommand();
            string expectedOtpId = "otp-gen-1234567"; // 15 ký tự

            // Giả lập sinh ID
            _mockIdGenerator.Setup(x => x.Generate(15))
                            .Returns(expectedOtpId);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Đã gửi OTP thành công (General).");

            // Verify: OTP được lưu xuống DB với thông tin đúng
            _mockOtpRepo.Verify(x => x.AddAsync(It.Is<Otp>(o =>
                o.OtpId == expectedOtpId &&
                o.Email == command.Email &&
                o.Type == OtpType.General &&       // Kiểm tra đúng loại General
                o.UserId == null &&                // General thường không cần UserId (hoặc null như trong code)
                o.Status == OtpStatus.Active &&
                o.OtpCode.Length == 6              // Kiểm tra mã random có 6 số
            )), Times.Once);

            // Verify: Đã gọi SaveChanges
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify: Đã gửi email
            _mockEmailService.Verify(x => x.SendEmailAsync(
                command.Email,
                "Mã xác thực (General)", // Subject
                It.Is<string>(b => b.Contains("Mã xác thực của bạn là")) // Body chứa nội dung cần thiết
            ), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_DatabaseFails()
        {
            // 1. Arrange
            var command = OtpTestData.GetValidGeneralOtpCommand();

            // Giả lập lỗi khi lưu DB
            _mockOtpRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("DB Connection Lost"));

            // 2. Act & Assert
            // Vì handler không có try-catch, nên mong đợi nó ném Exception ra ngoài
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("DB Connection Lost");

            // Đảm bảo Email KHÔNG được gửi nếu lưu DB thất bại (Logic transaction cơ bản)
            // Lưu ý: Trong code của bạn, hàm gửi email nằm SAU hàm SaveChanges, 
            // nên nếu SaveChanges lỗi thì dòng gửi email sẽ không bao giờ chạy -> Test này đúng.
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}