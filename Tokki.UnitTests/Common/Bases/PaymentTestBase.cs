using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Payments.Commands.CreatePayment;

namespace Tokki.UnitTests.Common.Bases
{
    public class PaymentTestBase
    {
        protected readonly Mock<IPaymentRepository> _mockPaymentRepo;
        protected readonly Mock<IVipPackageRepository> _mockVipRepo;
        protected readonly Mock<ISePayService> _mockSePayService;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;

        protected readonly CreatePaymentCommandHandler _handler;

        public PaymentTestBase()
        {
            _mockPaymentRepo = new Mock<IPaymentRepository>();
            _mockVipRepo = new Mock<IVipPackageRepository>();
            _mockSePayService = new Mock<ISePayService>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _mockIdGen.Setup(x => x.GenerateCustom(It.IsAny<int>()))
                      .Returns("PAY-MOCK-ID");

            _mockSePayService.Setup(x => x.GenerateQrUrl(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                             .Returns("https://qr.sepay.vn/mock-url");

            _handler = new CreatePaymentCommandHandler(
                _mockPaymentRepo.Object,
                _mockVipRepo.Object,
                _mockSePayService.Object,
                _mockIdGen.Object
            );
        }
    }
}