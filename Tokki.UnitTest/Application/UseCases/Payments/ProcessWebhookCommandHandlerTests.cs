using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Payments.Commands.ProcessWebhook;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments
{
    public class ProcessWebhookCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ProcessWebhookCommandHandler CreateHandler(
            Mock<IPaymentRepository>? paymentRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IVipPackageRepository>? vipRepo = null,
            Mock<IIdGeneratorService>? idGen = null)
        {
            return new ProcessWebhookCommandHandler(
                (paymentRepo ?? new Mock<IPaymentRepository>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object,
                (vipRepo     ?? new Mock<IVipPackageRepository>()).Object,
                (idGen       ?? new Mock<IIdGeneratorService>()).Object,
                new Mock<ILogger<ProcessWebhookCommandHandler>>().Object);
        }

        // Build a realistic webhook payload containing a 10-char paymentId
        private static SePayWebhookData BuildWebhookData(
            string content = "Thanh toan PAYID12345",
            decimal amount = 99_000m,
            string transferType = "in") => new()
        {
            Content        = content,
            TransferAmount = amount,
            TransferType   = transferType,
            Gateway        = "VCB",
            TransactionDate = "2025-01-01T00:00:00+07:00",
            AccountNumber  = "0123456789",
            SubAccount     = null,
            ReferenceCode  = "REF001",
            Description    = "SePay webhook"
        };

        private static ProcessWebhookCommand BuildCommand(SePayWebhookData? data = null) => new(data ?? BuildWebhookData());

        private static Payment BuildPayment(string id, PaymentStatus status = PaymentStatus.Pending, decimal amount = 99_000m) => new()
        {
            Id           = id,
            UserId       = "USER-001",
            Amount       = amount,
            Status       = status,
            VipPackageId = "PKG-001"
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-WHK-01 | 200 | Content has no paymentId → returns invalid content msg
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoPaymentIdInContent_ShouldReturnInvalidContentMessage()
        {
            // Arrange – content does not contain a 10-char alphanumeric token
            var data = BuildWebhookData(content: "GD den ngan hang");
            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            // Act
            var result = await CreateHandler(paymentRepo: mockPayRepo).Handle(BuildCommand(data), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(); // handler returns Success with informational message
            mockPayRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);

            QACollector.LogTestCase("Payments - Process Webhook", new TestCaseDetail
            {
                FunctionGroup     = "ProcessWebhook",
                TestCaseID        = "TC-PAY-WHK-01",
                Description       = "Webhook content has no 10-char paymentId → returns PaymentInvalidContent message",
                ExpectedResult    = "Return Success with informational message, GetByIdAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExtractPaymentId returns empty string" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-WHK-02 | 200 | PaymentId extracted but payment not found → informational
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentNotFound_ShouldReturnPaymentNotFoundMessage()
        {
            // Arrange – content has valid 10-char token "PAYID12345" but payment not in DB
            var data = BuildWebhookData(content: "Thanh toan PAYID12345");
            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            mockPayRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);

            // Act
            var result = await CreateHandler(paymentRepo: mockPayRepo).Handle(BuildCommand(data), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(); // informational success
            mockPayRepo.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);

            QACollector.LogTestCase("Payments - Process Webhook", new TestCaseDetail
            {
                FunctionGroup     = "ProcessWebhook",
                TestCaseID        = "TC-PAY-WHK-02",
                Description       = "PaymentId extracted from content but not found in DB → informational success",
                ExpectedResult    = "Return Success, UpdateAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-WHK-03 | 200 | Payment already Paid → returns AlreadyProcessed msg
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaymentAlreadyPaid_ShouldReturnAlreadyProcessedMessage()
        {
            // Arrange
            var paidPayment = BuildPayment("PAYID12345", PaymentStatus.Paid);
            var data = BuildWebhookData(content: "Thanh toan PAYID12345");

            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            mockPayRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(paidPayment);

            // Act
            var result = await CreateHandler(paymentRepo: mockPayRepo).Handle(BuildCommand(data), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockPayRepo.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);

            QACollector.LogTestCase("Payments - Process Webhook", new TestCaseDetail
            {
                FunctionGroup     = "ProcessWebhook",
                TestCaseID        = "TC-PAY-WHK-03",
                Description       = "Payment is already Paid → returns AlreadyProcessed, no update",
                ExpectedResult    = "Return Success (AlreadyProcessed msg), UpdateAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "payment.Status != Pending" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-WHK-04 | 200 | Pending + enough amount → Status=Paid, VIP activated
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingPaymentEnoughAmount_ShouldActivateVipAndMarkPaid()
        {
            // Arrange
            var payment = BuildPayment("PAYID12345", PaymentStatus.Pending, 99_000m);
            var vipPkg  = new VipPackage { Id = "PKG-001", DurationDays = 30, IsActive = true, Price = 99_000m };
            var user    = new Account { UserId = "USER-001", Role = AccountRole.User };

            var data = BuildWebhookData(content: "Thanh toan PAYID12345", amount: 99_000m);

            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            mockPayRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(payment);
            mockPayRepo.Setup(x => x.UpdateAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

            var mockVip = new Mock<IVipPackageRepository>();
            mockVip.Setup(x => x.GetByIdAsync("PKG-001")).ReturnsAsync(vipPkg);

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetByIdAsync("USER-001")).ReturnsAsync(user);
            mockAccount.Setup(x => x.UpdateUserAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

            // Act
            var result = await CreateHandler(
                paymentRepo: mockPayRepo,
                accountRepo: mockAccount,
                vipRepo: mockVip
            ).Handle(BuildCommand(data), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Paid);
            user.Role.Should().Be(AccountRole.Vip);
            mockPayRepo.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Once);
            mockAccount.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);

            QACollector.LogTestCase("Payments - Process Webhook", new TestCaseDetail
            {
                FunctionGroup     = "ProcessWebhook",
                TestCaseID        = "TC-PAY-WHK-04",
                Description       = "Pending payment, amount >= required → Status=Paid, user becomes VIP",
                ExpectedResult    = "Payment.Status=Paid, user.Role=Vip, UpdateAsync called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "status==Pending && amount>=required" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-WHK-05 | 200 | Pending + insufficient amount → informational msg
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingPaymentInsufficientAmount_ShouldReturnInsufficientMsg()
        {
            // Arrange – only sent 50,000 but required 99,000
            var payment = BuildPayment("PAYID12345", PaymentStatus.Pending, 99_000m);
            var data    = BuildWebhookData(content: "Thanh toan PAYID12345", amount: 50_000m);

            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            mockPayRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(payment);

            // Act
            var result = await CreateHandler(paymentRepo: mockPayRepo).Handle(BuildCommand(data), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Pending); // still pending
            mockPayRepo.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);

            QACollector.LogTestCase("Payments - Process Webhook", new TestCaseDetail
            {
                FunctionGroup     = "ProcessWebhook",
                TestCaseID        = "TC-PAY-WHK-05",
                Description       = "Transfer amount (50,000) < required (99,000) → not activated, still Pending",
                ExpectedResult    = "Return Success with InsufficientAmount msg, Payment still Pending",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "data.TransferAmount < payment.Amount" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-PAY-WHK-06 | 500 | AddTransactionAsync throws → returns ServerError
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AddTransactionThrows_ShouldReturn500()
        {
            // Arrange
            var mockPayRepo = new Mock<IPaymentRepository>();
            mockPayRepo.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>()))
                       .ThrowsAsync(new Exception("DB connection timeout"));

            // Act
            var result = await CreateHandler(paymentRepo: mockPayRepo).Handle(BuildCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Payments - Process Webhook", new TestCaseDetail
            {
                FunctionGroup     = "ProcessWebhook",
                TestCaseID        = "TC-PAY-WHK-06",
                Description       = "AddTransactionAsync throws → handler catches and returns ServerError 500",
                ExpectedResult    = "Return 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception) => Failure(ServerError, 500)" }
            });
        }
    }
}
