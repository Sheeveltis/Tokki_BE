using Microsoft.Extensions.Logging;
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Application.UseCases.Payments.Commands.ProcessWebhook;

namespace Tokki.UnitTests.Common.Bases
{
    public class PaymentTestBase
    {
        protected readonly Mock<IPaymentRepository> _mockPaymentRepo;
        protected readonly Mock<IVipPackageRepository> _mockVipRepo;
        protected readonly Mock<IAccountRepository> _mockAccountRepo;       

        protected readonly Mock<ISePayService> _mockSePayService;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;

        protected readonly Mock<ILogger<ProcessWebhookCommandHandler>> _mockWebhookLogger;
        protected readonly Mock<ILogger<CreatePaymentCommandHandler>> _mockCreateLogger; 

        public PaymentTestBase()
        {
            _mockPaymentRepo = new Mock<IPaymentRepository>();
            _mockVipRepo = new Mock<IVipPackageRepository>();
            _mockAccountRepo = new Mock<IAccountRepository>();

            _mockSePayService = new Mock<ISePayService>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _mockWebhookLogger = new Mock<ILogger<ProcessWebhookCommandHandler>>();
            _mockCreateLogger = new Mock<ILogger<CreatePaymentCommandHandler>>();

            _mockIdGen.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("MOCK_GEN_ID");
        }
    }
}