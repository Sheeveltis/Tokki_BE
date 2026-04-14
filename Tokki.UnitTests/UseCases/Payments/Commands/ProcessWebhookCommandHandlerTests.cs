using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Payments.Commands.ProcessWebhook;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Payments.Commands
{
    public class ProcessWebhookCommandHandlerTests : PaymentTestBase
    {
        private readonly ProcessWebhookCommandHandler _webhookHandler;

        public ProcessWebhookCommandHandlerTests()
        {
            _webhookHandler = new ProcessWebhookCommandHandler(
                _mockPaymentRepo.Object,
                _mockAccountRepo.Object,
                _mockVipRepo.Object,
                _mockIdGen.Object,
                _mockWebhookLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_NoPaymentIdFoundInContent()
        {
            var command = PaymentTestData.GetWebhookCommand("Chuyen tien cho vui", 50000);

            var result = await _webhookHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(AppErrors.PaymentInvalidContent.Description);

            _mockPaymentRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_PaymentIdNotFoundInDb()
        {
            var fakeId = "PAY1234567";
            var command = PaymentTestData.GetWebhookCommand($"Thanh toan {fakeId}", 50000);

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(fakeId)).ReturnsAsync((Payment?)null);

            var result = await _webhookHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); 
            result.Data.Should().Be(AppErrors.PaymentNotFound.Description);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_PaymentAlreadyProcessed()
        {
            var paymentId = "PAY_DONE_1";
            var command = PaymentTestData.GetWebhookCommand($"Thanh toan {paymentId}", 50000);
            var existingPayment = PaymentTestData.GetPayment(paymentId, 50000, PaymentStatus.Paid);

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId)).ReturnsAsync(existingPayment);

            var result = await _webhookHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(AppErrors.PaymentAlreadyProcessed.Description);

            _mockPaymentRepo.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_AmountIsInsufficient()
        {
            var paymentId = "PAY_LOW_01";
            var command = PaymentTestData.GetWebhookCommand($"Thanh toan {paymentId}", 20000); 
            var payment = PaymentTestData.GetPayment(paymentId, 50000, PaymentStatus.Pending); 

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId)).ReturnsAsync(payment);

            var result = await _webhookHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("lower than package value");

            payment.Status.Should().Be(PaymentStatus.Pending);
            _mockPaymentRepo.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ActivateVip_When_PaymentIsValid()
        {
            var paymentId = "PAY_OK_001";
            var userId = "user-vip-01";
            var vipPkgId = "vip-goi-thang";

            var command = PaymentTestData.GetWebhookCommand($"Thanh toan {paymentId}", 50000);
            var payment = PaymentTestData.GetPayment(paymentId, 50000, PaymentStatus.Pending);
            var vipPackage = PaymentTestData.GetActiveVipPackage(); 
            var user = PaymentTestData.GetUser(userId, AccountRole.User); 

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId)).ReturnsAsync(payment);
            _mockVipRepo.Setup(x => x.GetByIdAsync(vipPkgId)).ReturnsAsync(vipPackage);
            _mockAccountRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _webhookHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(OperationMessages.PaymentSuccess());

            payment.Status.Should().Be(PaymentStatus.Paid);
            _mockPaymentRepo.Verify(x => x.UpdateAsync(payment), Times.Once);

            user.Role.Should().Be(AccountRole.Vip);
            user.VipExpirationDate.Should().NotBeNull();
            user.VipExpirationDate.Value.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ExtendVip_When_UserIsAlreadyVip()
        {
            var paymentId = "PAY_EXTEND";
            var command = PaymentTestData.GetWebhookCommand($"Thanh toan {paymentId}", 50000);
            var payment = PaymentTestData.GetPayment(paymentId, 50000, PaymentStatus.Pending);
            var vipPackage = PaymentTestData.GetActiveVipPackage(); 

            var user = PaymentTestData.GetUser("user-vip", AccountRole.Vip);
            var currentExpiry = user.VipExpirationDate.Value;

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId)).ReturnsAsync(payment);
            _mockVipRepo.Setup(x => x.GetByIdAsync(payment.VipPackageId)).ReturnsAsync(vipPackage);
            _mockAccountRepo.Setup(x => x.GetByIdAsync(payment.UserId)).ReturnsAsync(user);

            await _webhookHandler.Handle(command, CancellationToken.None);

            
            var expectedExpiry = currentExpiry.AddDays(30);
            user.VipExpirationDate.Value.Should().Be(expectedExpiry);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_VipPackageDataIsMissing()
        {
            var paymentId = "PAY_ERR_01";
            var command = PaymentTestData.GetWebhookCommand($"Thanh toan {paymentId}", 50000);
            var payment = PaymentTestData.GetPayment(paymentId, 50000, PaymentStatus.Pending);

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId)).ReturnsAsync(payment);
            _mockVipRepo.Setup(x => x.GetByIdAsync(payment.VipPackageId)).ReturnsAsync((VipPackage?)null); // Lỗi ở đây

            var result = await _webhookHandler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageNotFound.Code);
        }
    }
}