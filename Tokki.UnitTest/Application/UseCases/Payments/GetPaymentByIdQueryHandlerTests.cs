using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments
{
    public class GetPaymentByIdQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static GetPaymentByIdQueryHandler CreateHandler(
            Mock<IPaymentRepository>? paymentRepo = null)
        {
            return new GetPaymentByIdQueryHandler(
                (paymentRepo ?? new Mock<IPaymentRepository>()).Object);
        }

        private static Payment BuildPayment(string id = "PAY-001") => new()
        {
            Id           = id,
            UserId       = "USER-001",
            Amount       = 99_000m,
            Status       = PaymentStatus.Pending,
            Description  = "VIP 1 Month",
            VipPackageId = "PKG-001",
            CreatedAt    = DateTimeOffset.UtcNow
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-GBI-01 | 404 | Payment not found → Failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentNotFound_ShouldReturn404()
        {
            // Arrange
            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);

            var query = new GetPaymentByIdQuery { Id = "NONEXISTENT" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Payments - Get Payment By ID", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentById",
                TestCaseID        = "TC-PAY-GBI-01",
                Description       = "PaymentId does not exist → 404 PaymentNotFound",
                ExpectedResult    = "Return 404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-GBI-02 | 200 | Payment found → returned correctly
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentFound_ShouldReturn200WithPayment()
        {
            // Arrange
            var payment = BuildPayment("PAY-001");
            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("PAY-001")).ReturnsAsync(payment);

            var query = new GetPaymentByIdQuery { Id = "PAY-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Id.Should().Be("PAY-001");
            result.Data.Amount.Should().Be(99_000m);

            QACollector.LogTestCase("Payments - Get Payment By ID", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentById",
                TestCaseID        = "TC-PAY-GBI-02",
                Description       = "Payment found → return 200 with Payment entity",
                ExpectedResult    = "Return 200, Payment.Id='PAY-001', Amount=99000",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment != null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-GBI-03 | 200 | Payment status Pending → returned
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingPayment_ShouldReturnWithPendingStatus()
        {
            // Arrange
            var payment = BuildPayment();
            payment.Status = PaymentStatus.Pending;

            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(payment);

            // Act
            var result = await CreateHandler(mockRepo).Handle(new GetPaymentByIdQuery { Id = "PAY-001" }, CancellationToken.None);

            // Assert
            result.Data!.Status.Should().Be(PaymentStatus.Pending);

            QACollector.LogTestCase("Payments - Get Payment By ID", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentById",
                TestCaseID        = "TC-PAY-GBI-03",
                Description       = "Payment with Pending status → Status returned correctly",
                ExpectedResult    = "Payment.Status = Pending",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment.Status = Pending" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-GBI-04 | 200 | Payment status Paid → returned
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaidPayment_ShouldReturnWithPaidStatus()
        {
            // Arrange
            var payment = BuildPayment();
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTimeOffset.UtcNow;

            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(payment);

            // Act
            var result = await CreateHandler(mockRepo).Handle(new GetPaymentByIdQuery { Id = "PAY-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Status.Should().Be(PaymentStatus.Paid);
            result.Data.PaidAt.Should().NotBeNull();

            QACollector.LogTestCase("Payments - Get Payment By ID", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentById",
                TestCaseID        = "TC-PAY-GBI-04",
                Description       = "Payment already Paid → Status=Paid and PaidAt is set",
                ExpectedResult    = "Payment.Status = Paid, PaidAt != null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment.Status = Paid && PaidAt != null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-GBI-05 | 200 | UserId and VipPackageId mapped correctly
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidPayment_ShouldMapUserIdAndVipPackageId()
        {
            // Arrange
            var payment = new Payment
            {
                Id           = "PAY-DETAIL",
                UserId       = "USER-XYZ",
                VipPackageId = "PKG-999",
                Amount       = 149_000m,
                Status       = PaymentStatus.Pending,
                CreatedAt    = DateTimeOffset.UtcNow,
                Description  = "Test"
            };

            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("PAY-DETAIL")).ReturnsAsync(payment);

            // Act
            var result = await CreateHandler(mockRepo).Handle(new GetPaymentByIdQuery { Id = "PAY-DETAIL" }, CancellationToken.None);

            // Assert
            result.Data!.UserId.Should().Be("USER-XYZ");
            result.Data.VipPackageId.Should().Be("PKG-999");
            result.Data.Amount.Should().Be(149_000m);

            QACollector.LogTestCase("Payments - Get Payment By ID", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentById",
                TestCaseID        = "TC-PAY-GBI-05",
                Description       = "UserId, VipPackageId and Amount mapped correctly from entity",
                ExpectedResult    = "UserId='USER-XYZ', VipPackageId='PKG-999', Amount=149000",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment fields mapped as-is" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-GBI-06 | 500 | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var mockRepo = new Mock<IPaymentRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB timeout"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(new GetPaymentByIdQuery { Id = "PAY-001" }, CancellationToken.None));

            QACollector.LogTestCase("Payments - Get Payment By ID", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentById",
                TestCaseID        = "TC-PAY-GBI-06",
                Description       = "Repository throws exception → propagates unhandled",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws" }
            });
        }
    }
}
