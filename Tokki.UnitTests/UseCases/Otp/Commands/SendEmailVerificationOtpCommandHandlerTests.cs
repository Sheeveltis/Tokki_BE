using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.IRepositories; // Nhớ thêm dòng này để dùng IAccountRepository
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Otps.Commands
{
    public class SendEmailVerificationOtpCommandHandlerTests : OtpTestBase
    {
        private readonly SendGeneralOtpCommandHandler _handler;
        private readonly Mock<IValidator<SendEmailVerificationOtpCommand>> _mockValidator;

        // 1. Khai báo thêm Mock cho Account Repository
        private readonly Mock<IAccountRepository> _mockAccountRepo;

        public SendEmailVerificationOtpCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<SendEmailVerificationOtpCommand>>();

            // 2. Khởi tạo Mock
            _mockAccountRepo = new Mock<IAccountRepository>();

            // 3. Cập nhật Constructor khớp với thay đổi bên file Handler
            _handler = new SendGeneralOtpCommandHandler(
                _mockOtpRepo.Object,
                _mockAccountRepo.Object, // <--- THÊM VÀO ĐÂY (Vị trí số 2)
                _mockEmailService.Object,
                _mockValidator.Object,
                _mockSystemConfigRepo.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountIsBanned()
        {
            // 1. Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            // Giả lập: Tìm thấy tài khoản đang bị BANNED
            var bannedAccount = new Account
            {
                Email = command.Email,
                Status = AccountStatus.Banned // <--- Quan trọng
                                              // Không có IsDeleted
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(bannedAccount);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountUnavailable.Code);

            // Verify: Không được gửi email
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountIsActive()
        {
            // 1. Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            // Giả lập: Tài khoản đã ACTIVE (Đã đăng ký rồi)
            var activeAccount = new Account
            {
                Email = command.Email,
                Status = AccountStatus.Active // <--- Quan trọng
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(activeAccount);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailAlreadyExists.Code);
        }

        [Fact]
        public async Task Handle_Should_Success_When_AccountIsInactive()
        {
            // 1. Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            // Giả lập: Tài khoản INACTIVE (Đăng ký nhưng chưa verify -> Cho phép gửi lại OTP)
            var inactiveAccount = new Account
            {
                Email = command.Email,
                Status = AccountStatus.Inactive
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(inactiveAccount); // Tìm thấy account inactive

            // Setup các phần khác để chạy thành công
            _mockOtpRepo.Setup(x => x.GetLatestValidOtpAsync(It.IsAny<string>(), It.IsAny<OtpType>()))
                        .ReturnsAsync((Otp?)null);
            _mockIdGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns("otp-123");

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue(); // Phải thành công

            // Verify: Email service PHẢI được gọi
            _mockEmailService.Verify(x => x.SendEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}