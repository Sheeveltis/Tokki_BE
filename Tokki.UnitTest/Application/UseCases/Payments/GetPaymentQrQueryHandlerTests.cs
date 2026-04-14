using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentQr;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments
{
    public class GetPaymentQrQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static GetPaymentQrQueryHandler CreateHandler(
            Mock<IPaymentRepository>? paymentRepo = null,
            Mock<ISePayService>? sePay = null)
        {
            return new GetPaymentQrQueryHandler(
                (paymentRepo ?? new Mock<IPaymentRepository>()).Object,
                (sePay       ?? new Mock<ISePayService>()).Object);
        }

        private static Payment BuildPayment(string id = "PAY-001", decimal amount = 99_000m) => new()
        {
            Id          = id,
            UserId      = "USER-001",
            Amount      = amount,
            Description = $"Thanh toán {id}",
            Status      = PaymentStatus.Pending,
            VipPackageId = "PKG-001"
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-QR-01 | 404 | Payment not found → Failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentNotFound_ShouldReturn404()
        {
            // Arrange
            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);

            var query = new GetPaymentQrQuery("INVALID");


            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Payments - Get QR", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentQr",
                TestCaseID        = "TC-PAY-QR-01",
                Description       = "PaymentId does not exist → 404 PaymentNotFound",
                ExpectedResult    = "Return 404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-QR-02 | 200 | Payment found → QR URL returned
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentFound_ShouldReturnQrUrl()
        {
            // Arrange
            var payment = BuildPayment("PAY-001", 99_000m);

            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("PAY-001")).ReturnsAsync(payment);

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl("PAY-001", 99_000m, It.IsAny<string>()))
                     .Returns("https://qr.sepay.vn/PAY-001");

            var query = new GetPaymentQrQuery("PAY-001");

            // Act
            var result = await CreateHandler(mockRepo, mockSePay).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("https://qr.sepay.vn/PAY-001");

            QACollector.LogTestCase("Payments - Get QR", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentQr",
                TestCaseID        = "TC-PAY-QR-02",
                Description       = "Payment found → GenerateQrUrl called, URL returned",
                ExpectedResult    = "Return 200, Data = QR URL",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment != null => GenerateQrUrl" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-QR-03 | 200 | GenerateQrUrl called with correct payment fields
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentFound_ShouldPassCorrectFieldsToGenerateQrUrl()
        {
            // Arrange
            var payment = new Payment
            {
                Id          = "PRECISE-ID",
                Amount      = 199_000m,
                Description = "Thanh toán PRECISE-ID",
                Status      = PaymentStatus.Pending,
                UserId      = "USER-001",
                VipPackageId = "PKG-001"
            };

            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("PRECISE-ID")).ReturnsAsync(payment);

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                     .Returns("https://qr.example.com");

            var query = new GetPaymentQrQuery("PRECISE-ID");

            // Act
            await CreateHandler(mockRepo, mockSePay).Handle(query, CancellationToken.None);

            // Assert – exact parameters verified
            mockSePay.Verify(x => x.GenerateQrUrl("PRECISE-ID", 199_000m, "Thanh toán PRECISE-ID"), Times.Once);

            QACollector.LogTestCase("Payments - Get QR", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentQr",
                TestCaseID        = "TC-PAY-QR-03",
                Description       = "GenerateQrUrl called with exact id, amount, description from Payment entity",
                ExpectedResult    = "GenerateQrUrl('PRECISE-ID', 199000, 'Thanh toán PRECISE-ID') called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GenerateQrUrl(payment.Id, payment.Amount, payment.Description)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-QR-04 | 200 | Different payment ID → different QR URL
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DifferentPaymentId_ShouldReturnDistinctQrUrl()
        {
            // Arrange
            var payment2 = BuildPayment("PAY-002", 49_000m);

            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("PAY-002")).ReturnsAsync(payment2);

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl("PAY-002", It.IsAny<decimal>(), It.IsAny<string>()))
                     .Returns("https://qr.sepay.vn/PAY-002");

            var query = new GetPaymentQrQuery("PAY-002");

            // Act
            var result = await CreateHandler(mockRepo, mockSePay).Handle(query, CancellationToken.None);

            // Assert
            result.Data.Should().Be("https://qr.sepay.vn/PAY-002");

            QACollector.LogTestCase("Payments - Get QR", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentQr",
                TestCaseID        = "TC-PAY-QR-04",
                Description       = "Different PaymentId produces distinct QR URL",
                ExpectedResult    = "QR URL contains 'PAY-002'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Each paymentId → unique QR URL" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-QR-05 | 500 | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(new GetPaymentQrQuery ("PAY-001"), CancellationToken.None));

            QACollector.LogTestCase("Payments - Get QR", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentQr",
                TestCaseID        = "TC-PAY-QR-05",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws DB error" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-QR-06 | 500 | GenerateQrUrl throws → exception propagates
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_GenerateQrUrlThrows_ShouldPropagateException()
        {
            // Arrange
            var payment = BuildPayment();
            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(payment);

            var mockSePay = new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                     .Throws(new Exception("SePay service unreachable"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo, mockSePay).Handle(new GetPaymentQrQuery ("PAY-001"), CancellationToken.None));

            QACollector.LogTestCase("Payments - Get QR", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentQr",
                TestCaseID        = "TC-PAY-QR-06",
                Description       = "GenerateQrUrl throws (SePay unreachable) → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "sePayService.GenerateQrUrl throws" }
            });
        }
    }
}