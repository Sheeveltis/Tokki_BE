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
        private GetPaymentQrQueryHandler CreateHandler(
            Mock<IPaymentRepository>? paymentRepo = null,
            Mock<ISePayService>? sePayService = null)
        {
            var mockSePay = sePayService ?? new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl(
                        It.IsAny<string>(),
                        It.IsAny<decimal>(),
                        It.IsAny<string>()))
                     .Returns("https://sepay.vn/qr/fake-url");

            return new GetPaymentQrQueryHandler(
                (paymentRepo ?? new Mock<IPaymentRepository>()).Object,
                mockSePay.Object);
        }

        [Fact]
        public async Task Handle_PaymentNotFound_ShouldReturn404()
        {
            // Arrange
            var query = new GetPaymentQrQuery("PAY-INVALID");

            var mockPaymentRepo = new Mock<IPaymentRepository>();
            mockPaymentRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                           .ReturnsAsync((Payment?)null);

            var handler = CreateHandler(paymentRepo: mockPaymentRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Payment - Get QR", new TestCaseDetail
            {
                FunctionGroup = "Get Payment QR",
                TestCaseID = "TC-PAY-QR-01",
                Description = "Lấy QR với PaymentId không tồn tại",
                ExpectedResult = "Return 404 PaymentNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid PaymentId",
                    "Payment = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPayment_ShouldReturnQrUrl()
        {
            // Arrange
            var query = new GetPaymentQrQuery("PAY-001");

            var payment = new Payment
            {
                Id = "PAY-001",
                UserId = "USER-001",
                Amount = 99000,
                Description = "Thanh toán PAY-001",
                Status = PaymentStatus.Pending
            };

            var mockPaymentRepo = new Mock<IPaymentRepository>();
            mockPaymentRepo.Setup(x => x.GetByIdAsync("PAY-001"))
                           .ReturnsAsync(payment);

            var handler = CreateHandler(paymentRepo: mockPaymentRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain("sepay.vn");

            QACollector.LogTestCase("Payment - Get QR", new TestCaseDetail
            {
                FunctionGroup = "Get Payment QR",
                TestCaseID = "TC-PAY-QR-02",
                Description = "Lấy QR với PaymentId hợp lệ → trả về QR URL",
                ExpectedResult = "Return 200, Data chứa 'sepay.vn'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid PaymentId",
                    "Payment tồn tại",
                    "GenerateQrUrl called",
                    "Return 200"
                }
            });
        }
    }
}