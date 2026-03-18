using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockOtpRepository
    {
        public static Mock<IOtpRepository> GetMock(Otp? latestOtp = null)
        {
            var mock = new Mock<IOtpRepository>();

            mock.Setup(x => x.GetLatestValidOtpAsync(
                        It.IsAny<string>(),
                        It.IsAny<OtpType>()))
                .ReturnsAsync(latestOtp);

            mock.Setup(x => x.AddAsync(It.IsAny<Otp>()))
                .Returns(Task.CompletedTask);

            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        public static Otp GetSampleOtp(string email = "test@tokki.com")
        {
            return new Otp
            {
                OtpId = "OTP-001",
                Email = email,
                OtpCode = "123456",
                Type = OtpType.VerifyEmail,
                Status = OtpStatus.Active,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddMinutes(5)
            };
        }

        // OTP vừa gửi 10 giây trước → rate limit
        public static Otp GetRecentOtp(string email = "test@tokki.com")
        {
            return new Otp
            {
                OtpId = "OTP-002",
                Email = email,
                OtpCode = "654321",
                Type = OtpType.VerifyEmail,
                Status = OtpStatus.Active,
                CreatedAt = DateTime.UtcNow.AddHours(7).AddSeconds(-10), // 10 giây trước
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddMinutes(5)
            };
        }
    }
}