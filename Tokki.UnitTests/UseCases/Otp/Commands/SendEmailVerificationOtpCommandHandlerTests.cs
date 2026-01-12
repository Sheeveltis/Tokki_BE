using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp;
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
        private readonly SendGeneralOtpCommandHandler _handler;
        private readonly Mock<IValidator<SendEmailVerificationOtpCommand>> _mockValidator;
        private readonly Mock<IAccountRepository> _mockAccountRepo;

        public SendEmailVerificationOtpCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<SendEmailVerificationOtpCommand>>();
            _mockAccountRepo = new Mock<IAccountRepository>();

            _handler = new SendGeneralOtpCommandHandler(
                _mockOtpRepo.Object,
                _mockAccountRepo.Object,
                _mockEmailService.Object,
                _mockValidator.Object,
                _mockSystemConfigRepo.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountIsBanned()
        {
            // Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            var bannedAccount = new Account
            {
                Email = command.Email,
                Status = AccountStatus.Banned
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(bannedAccount);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountUnavailable.Code);

            _mockEmailService.Verify(
                x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never
            );

            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountIsActive()
        {
            // Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            var activeAccount = new Account
            {
                Email = command.Email,
                Status = AccountStatus.Active
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(activeAccount);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailAlreadyExists.Code);

            _mockEmailService.Verify(
                x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never
            );

            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountIsInactive()
        {
            // Arrange
            // LƯU Ý: theo handler hiện tại, Inactive cũng bị chặn (AccountUnavailable)
            var command = OtpTestData.GetEmailVerificationCommand();

            var inactiveAccount = new Account
            {
                Email = command.Email,
                Status = AccountStatus.Inactive
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync(inactiveAccount);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.AccountUnavailable.Code);

            _mockEmailService.Verify(
                x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never
            );

            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Success_When_AccountNotExists_And_NoRateLimit()
        {
            // Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            // Muốn success => account phải null
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync((Account?)null);

            _mockOtpRepo.Setup(x => x.GetLatestValidOtpAsync(command.Email, OtpType.VerifyEmail))
                        .ReturnsAsync((Otp?)null);

            _mockSystemConfigRepo.Setup(x => x.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS"))
                                 .ReturnsAsync("300");

            _mockIdGenerator.Setup(x => x.Generate(15)).Returns("otp-123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("Mã OTP đã được gửi đến email của bạn.");

            _mockOtpRepo.Verify(x => x.AddAsync(It.Is<Otp>(o =>
                o.OtpId == "otp-123" &&
                o.Email == command.Email &&
                o.Type == OtpType.VerifyEmail &&
                o.Status == OtpStatus.Active &&
                !string.IsNullOrEmpty(o.OtpCode) &&
                o.OtpCode.Length == 6
            )), Times.Once);

            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockEmailService.Verify(
                x => x.SendEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RateLimitExceeded()
        {
            // Arrange
            var command = OtpTestData.GetEmailVerificationCommand();

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(command.Email))
                            .ReturnsAsync((Account?)null);

            // OTP vừa tạo cách đây 10s => bị rate limit (min 60s)
            var recentOtp = new Otp
            {
                Email = command.Email,
                Type = OtpType.VerifyEmail,
                CreatedAt = DateTime.UtcNow.AddHours(7).AddSeconds(-10),
                Status = OtpStatus.Active
            };

            _mockOtpRepo.Setup(x => x.GetLatestValidOtpAsync(command.Email, OtpType.VerifyEmail))
                        .ReturnsAsync(recentOtp);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(429);

            _mockOtpRepo.Verify(x => x.AddAsync(It.IsAny<Otp>()), Times.Never);
            _mockOtpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(
                x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never
            );
        }
    }
}
