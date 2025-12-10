using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.ForgotPassword;
using Microsoft.Extensions.Logging; // Nếu cần logger sau này

namespace Tokki.UnitTests.Common.Bases
{
    public class OtpTestBase
    {
        // Mock các Repository và Service
        protected readonly Mock<IAccountRepository> _mockAccountRepo;
        protected readonly Mock<IOtpRepository> _mockOtpRepo;
        protected readonly Mock<IEmailService> _mockEmailService;
        protected readonly Mock<ISystemConfigRepository> _mockSystemConfigRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;

        // Handler cần test
        protected readonly SendForgotPasswordOtpCommandHandler _handler;

        public OtpTestBase()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockOtpRepo = new Mock<IOtpRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockSystemConfigRepo = new Mock<ISystemConfigRepository>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();

            _handler = new SendForgotPasswordOtpCommandHandler(
                _mockAccountRepo.Object,
                _mockOtpRepo.Object,
                _mockEmailService.Object,
                _mockSystemConfigRepo.Object,
                _mockIdGenerator.Object
            );
        }
    }
}