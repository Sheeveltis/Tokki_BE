using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentHistory;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments
{
    public class GetPaymentHistoryQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static GetPaymentHistoryQueryHandler CreateHandler(
            Mock<IPaymentRepository>? paymentRepo = null,
            Mock<IAccountRepository>? accountRepo = null)
        {
            return new GetPaymentHistoryQueryHandler(
                (paymentRepo ?? new Mock<IPaymentRepository>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object);
        }

        private static GetPaymentHistoryQuery ValidQuery => new("USER-001");

        private static Payment BuildPayment(string id, PaymentStatus status = PaymentStatus.Paid) => new()
        {
            Id           = id,
            UserId       = "USER-001",
            Amount       = 99_000m,
            Description  = $"Thanh toán {id}",
            Status       = status,
            VipPackageId = "PKG-001",
            CreatedAt    = DateTimeOffset.UtcNow,
            PaidAt       = status == PaymentStatus.Paid ? DateTimeOffset.UtcNow : null
        };

        // ═══════════════════════════════════════════════════════════════════
        // GetPaymentHistory_01 | 200 | No payments for user → returns empty list
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoPayments_ShouldReturnEmptyList()
        {
            // Arrange
            var mockPay = new Mock<IPaymentRepository>();
            mockPay.Setup(x => x.GetByUserIdAsync("USER-001")).ReturnsAsync(new List<Payment>());

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync((Account?)null);

            // Act
            var result = await CreateHandler(mockPay, mockAccount).Handle(ValidQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentHistory",
                TestCaseID        = "GetPaymentHistory_01",
                Description       = "User has no payment records → returns empty list",
                ExpectedResult    = "Return 200, Data = empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payments.Count == 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // GetPaymentHistory_02 | 200 | Multiple payments → all mapped to DTO
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MultiplePayments_ShouldMapAllToDto()
        {
            // Arrange
            var payments = new List<Payment>
            {
                BuildPayment("PAY-001", PaymentStatus.Paid),
                BuildPayment("PAY-002", PaymentStatus.Pending)
            };

            var mockPay = new Mock<IPaymentRepository>();
            mockPay.Setup(x => x.GetByUserIdAsync("USER-001")).ReturnsAsync(payments);

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync(new Account { UserId = "USER-001" });

            // Act
            var result = await CreateHandler(mockPay, mockAccount).Handle(ValidQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data[0].PaymentId.Should().Be("PAY-001");
            result.Data[1].PaymentId.Should().Be("PAY-002");

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentHistory",
                TestCaseID        = "GetPaymentHistory_02",
                Description       = "2 payments → both mapped to PaymentHistoryDTO",
                ExpectedResult    = "Return 200, Count=2, IDs correct",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payments.Select => DTO list" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // GetPaymentHistory_03 | 200 | VipExpirationDate from user → included in DTO
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserHasVipExpirationDate_ShouldIncludeInDto()
        {
            // Arrange
            var expiry = DateTimeOffset.UtcNow.AddDays(30);
            var user = new Account { UserId = "USER-001", VipExpirationDate = expiry };

            var mockPay = new Mock<IPaymentRepository>();
            mockPay.Setup(x => x.GetByUserIdAsync("USER-001"))
                   .ReturnsAsync(new List<Payment> { BuildPayment("PAY-001") });

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync(user);

            // Act
            var result = await CreateHandler(mockPay, mockAccount).Handle(ValidQuery, CancellationToken.None);

            // Assert
            result.Data[0].CurrentVipExpirationDate.Should().Be(expiry);

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentHistory",
                TestCaseID        = "GetPaymentHistory_03",
                Description       = "User has VipExpirationDate → included in every DTO row",
                ExpectedResult    = "DTO.CurrentVipExpirationDate = user.VipExpirationDate",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "user.VipExpirationDate propagated to DTO" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // GetPaymentHistory_04 | 200 | User not found → VipExpirationDate = null in DTO
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturnNullVipExpirationDate()
        {
            // Arrange
            var mockPay = new Mock<IPaymentRepository>();
            mockPay.Setup(x => x.GetByUserIdAsync("USER-001"))
                   .ReturnsAsync(new List<Payment> { BuildPayment("PAY-001") });

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync((Account?)null);

            // Act
            var result = await CreateHandler(mockPay, mockAccount).Handle(ValidQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data[0].CurrentVipExpirationDate.Should().BeNull();

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentHistory",
                TestCaseID        = "GetPaymentHistory_04",
                Description       = "Account not found → CurrentVipExpirationDate = null",
                ExpectedResult    = "DTO.CurrentVipExpirationDate = null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "user == null => VipExpirationDate = null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // GetPaymentHistory_05 | 200 | Payment fields correctly mapped to DTO
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentFields_ShouldBeMappedCorrectly()
        {
            // Arrange
            var paidAt = DateTimeOffset.UtcNow;
            var payment = new Payment
            {
                Id           = "PAY-FULL",
                Amount       = 299_000m,
                Description  = "3M VIP",
                Status       = PaymentStatus.Paid,
                VipPackageId = "PKG-003",
                CreatedAt    = DateTimeOffset.UtcNow.AddDays(-5),
                PaidAt       = paidAt,
                UserId       = "USER-001"
            };

            var mockPay = new Mock<IPaymentRepository>();
            mockPay.Setup(x => x.GetByUserIdAsync("USER-001")).ReturnsAsync(new List<Payment> { payment });

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync(new Account { UserId = "USER-001" });

            // Act
            var result = await CreateHandler(mockPay, mockAccount).Handle(ValidQuery, CancellationToken.None);

            // Assert
            var dto = result.Data[0];
            dto.PaymentId.Should().Be("PAY-FULL");
            dto.Amount.Should().Be(299_000m);
            dto.Description.Should().Be("3M VIP");
            dto.Status.Should().Be(PaymentStatus.Paid);
            dto.VipPackageId.Should().Be("PKG-003");
            dto.PaidAt.Should().Be(paidAt);

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentHistory",
                TestCaseID        = "GetPaymentHistory_05",
                Description       = "All payment fields correctly mapped to PaymentHistoryDTO",
                ExpectedResult    = "PaymentId, Amount, Description, Status, VipPackageId, PaidAt all correct",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "all Payment fields mapped via Select" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // GetPaymentHistory_06 | 500 | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var mockPay = new Mock<IPaymentRepository>();
            mockPay.Setup(x => x.GetByUserIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new Account());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockPay, mockAccount).Handle(ValidQuery, CancellationToken.None));

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup     = "GetPaymentHistory",
                TestCaseID        = "GetPaymentHistory_06",
                Description       = "GetByUserIdAsync throws → exception propagates unhandled",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByUserIdAsync throws" }
            });
        }
    }
}
