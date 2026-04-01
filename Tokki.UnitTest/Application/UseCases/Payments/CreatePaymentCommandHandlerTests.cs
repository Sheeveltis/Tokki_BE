using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments
{
    public class CreatePaymentCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static CreatePaymentCommandHandler CreateHandler(
            Mock<IPaymentRepository>? paymentRepo = null,
            Mock<IVipPackageRepository>? vipRepo = null,
            Mock<ISePayService>? sePay = null,
            Mock<IIdGeneratorService>? idGen = null)
        {
            return new CreatePaymentCommandHandler(
                (paymentRepo ?? new Mock<IPaymentRepository>()).Object,
                (vipRepo    ?? new Mock<IVipPackageRepository>()).Object,
                (sePay      ?? new Mock<ISePayService>()).Object,
                (idGen      ?? new Mock<IIdGeneratorService>()).Object);
        }

        private static VipPackage ActivePackage(string id = "PKG-001") => new()
        {
            Id          = id,
            Name        = "1 Tháng VIP",
            Price       = 99_000m,
            DurationDays = 30,
            IsActive    = true
        };

        private static CreatePaymentCommand ValidCommand => new()
        {
            UserId       = "USER-001",
            VipPackageId = "PKG-001"
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-CRT-01 | 404 | VipPackage not found → Failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VipPackageNotFound_ShouldReturn404()
        {
            // Arrange
            var mockVip = new Mock<IVipPackageRepository>();
            mockVip.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((VipPackage?)null);

            // Act
            var result = await CreateHandler(vipRepo: mockVip).Handle(ValidCommand, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Payments - Create Payment", new TestCaseDetail
            {
                FunctionGroup     = "CreatePayment",
                TestCaseID        = "TC-PAY-CRT-01",
                Description       = "VipPackageId does not exist in repository → 404 Failure",
                ExpectedResult    = "Return 404 VipPackageNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "vipPackage == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-CRT-02 | 400 | VipPackage inactive → Failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VipPackageInactive_ShouldReturn400()
        {
            // Arrange
            var inactivePkg = new VipPackage { Id = "PKG-001", IsActive = false, Price = 99_000m };
            var mockVip = new Mock<IVipPackageRepository>();
            mockVip.Setup(x => x.GetByIdAsync("PKG-001")).ReturnsAsync(inactivePkg);

            // Act
            var result = await CreateHandler(vipRepo: mockVip).Handle(ValidCommand, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Payments - Create Payment", new TestCaseDetail
            {
                FunctionGroup     = "CreatePayment",
                TestCaseID        = "TC-PAY-CRT-02",
                Description       = "VipPackage.IsActive = false → 400 VipPackageInactive",
                ExpectedResult    = "Return 400 VipPackageInactive",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!vipPackage.IsActive" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-CRT-03 | 201 | Happy path → Payment created, QR URL returned
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidPackage_ShouldCreatePaymentAndReturnQrUrl()
        {
            // Arrange
            var mockVip = new Mock<IVipPackageRepository>();
            mockVip.Setup(x => x.GetByIdAsync("PKG-001")).ReturnsAsync(ActivePackage());

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("PAY-ABC123");

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl("PAY-ABC123", 99_000m, It.IsAny<string>()))
                     .Returns("https://qr.sepay.vn/PAY-ABC123");

            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

            // Act
            var result = await CreateHandler(
                paymentRepo: mockPayRepo,
                vipRepo: mockVip,
                sePay: mockSePay,
                idGen: mockIdGen
            ).Handle(ValidCommand, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data!.PaymentId.Should().Be("PAY-ABC123");
            result.Data.PaymentUrl.Should().Be("https://qr.sepay.vn/PAY-ABC123");
            mockPayRepo.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Once);

            QACollector.LogTestCase("Payments - Create Payment", new TestCaseDetail
            {
                FunctionGroup     = "CreatePayment",
                TestCaseID        = "TC-PAY-CRT-03",
                Description       = "Valid active package → Payment created and QR URL returned",
                ExpectedResult    = "Return 201, PaymentId='PAY-ABC123', PaymentUrl=QR link",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "vipPackage found and active", "AddAsync called once" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-CRT-04 | 201 | Payment amount matches VipPackage price
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidPackage_PaymentAmountShouldMatchPackagePrice()
        {
            // Arrange
            var package = new VipPackage { Id = "PKG-001", Price = 199_000m, IsActive = true, Name = "3M VIP" };
            var mockVip = new Mock<IVipPackageRepository>();
            mockVip.Setup(x => x.GetByIdAsync("PKG-001")).ReturnsAsync(package);

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("PAY-XYZ");

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                     .Returns("https://qr.example.com");

            Payment? capturedPayment = null;
            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddAsync(It.IsAny<Payment>()))
                       .Callback<Payment>(p => capturedPayment = p)
                       .Returns(Task.CompletedTask);

            // Act
            await CreateHandler(paymentRepo: mockPayRepo, vipRepo: mockVip, sePay: mockSePay, idGen: mockIdGen)
                .Handle(ValidCommand, CancellationToken.None);

            // Assert
            capturedPayment!.Amount.Should().Be(199_000m);
            capturedPayment.UserId.Should().Be("USER-001");
            capturedPayment.VipPackageId.Should().Be("PKG-001");

            QACollector.LogTestCase("Payments - Create Payment", new TestCaseDetail
            {
                FunctionGroup     = "CreatePayment",
                TestCaseID        = "TC-PAY-CRT-04",
                Description       = "Payment.Amount must equal VipPackage.Price (199,000)",
                ExpectedResult    = "Payment.Amount = 199000, UserId and VipPackageId correctly set",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment.Amount = vipPackage.Price" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-CRT-05 | 500 | AddAsync throws → exception propagates
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AddAsyncThrows_ShouldPropagateException()
        {
            // Arrange
            var mockVip = new Mock<IVipPackageRepository>();
            mockVip.Setup(x => x.GetByIdAsync("PKG-001")).ReturnsAsync(ActivePackage());

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("PAY-ERR");

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                     .Returns("https://qr.example.com");

            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddAsync(It.IsAny<Payment>())).ThrowsAsync(new Exception("DB write failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(paymentRepo: mockPayRepo, vipRepo: mockVip, sePay: mockSePay, idGen: mockIdGen)
                    .Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Payments - Create Payment", new TestCaseDetail
            {
                FunctionGroup     = "CreatePayment",
                TestCaseID        = "TC-PAY-CRT-05",
                Description       = "AddAsync throws → exception propagates unhandled",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws DB write failed" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-CRT-06 | 201 | GenerateQrUrl called with correct paymentId
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidPackage_ShouldCallGenerateQrUrlWithPaymentId()
        {
            // Arrange
            var mockVip = new Mock<IVipPackageRepository>();
            mockVip.Setup(x => x.GetByIdAsync("PKG-001")).ReturnsAsync(ActivePackage());

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("PAYID-001");

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                     .Returns("https://qr.example.com/PAYID-001");

            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

            // Act
            await CreateHandler(paymentRepo: mockPayRepo, vipRepo: mockVip, sePay: mockSePay, idGen: mockIdGen)
                .Handle(ValidCommand, CancellationToken.None);

            // Assert – GenerateQrUrl must be called with exact paymentId (from IdGenerator)
            mockSePay.Verify(x => x.GenerateQrUrl("PAYID-001", 99_000m, It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("Payments - Create Payment", new TestCaseDetail
            {
                FunctionGroup     = "CreatePayment",
                TestCaseID        = "TC-PAY-CRT-06",
                Description       = "GenerateQrUrl called with paymentId from IdGenerator and correct amount",
                ExpectedResult    = "GenerateQrUrl(paymentId='PAYID-001', amount=99000) called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "sePayService.GenerateQrUrl(paymentId, vipPackage.Price, desc)" }
            });
        }
    }
}