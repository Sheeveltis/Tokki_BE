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
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments
{
    public class CreatePaymentCommandHandlerTests
    {
        private CreatePaymentCommandHandler CreateHandler(
            Mock<IPaymentRepository>? paymentRepo = null,
            Mock<IVipPackageRepository>? vipRepo = null,
            Mock<ISePayService>? sePayService = null)
        {
            var mockSePay = sePayService ?? new Mock<ISePayService>();
            mockSePay.Setup(x => x.GenerateQrUrl(
                        It.IsAny<string>(),
                        It.IsAny<decimal>(),
                        It.IsAny<string>()))
                     .Returns("https://sepay.vn/qr/fake-url");

            var mockPayment = paymentRepo ?? new Mock<IPaymentRepository>();
            mockPayment.Setup(x => x.AddAsync(It.IsAny<Payment>()))
                       .Returns(Task.CompletedTask);

            return new CreatePaymentCommandHandler(
                mockPayment.Object,
                (vipRepo ?? MockVipPackageRepository.GetMock()).Object,
                mockSePay.Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_VipPackageNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new CreatePaymentCommand
            {
                UserId = "USER-001",
                VipPackageId = "PKG-INVALID"
            };

            var handler = CreateHandler(
                vipRepo: MockVipPackageRepository.GetMock(returnedPackage: null));

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Payment - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Payment",
                TestCaseID = "TC-PAY-CRE-01",
                Description = "Tạo payment với VipPackageId không tồn tại",
                ExpectedResult = "Return 404 VipPackageNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid VipPackageId",
                    "VipPackage = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_VipPackageInactive_ShouldReturn400()
        {
            // Arrange — package tồn tại nhưng IsActive = false
            var command = new CreatePaymentCommand
            {
                UserId = "USER-001",
                VipPackageId = "PKG-002"
            };

            var inactivePackage = MockVipPackageRepository.GetSampleInactivePackage();

            var handler = CreateHandler(
                vipRepo: MockVipPackageRepository.GetMock(
                    returnedPackage: inactivePackage));

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Payment - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Payment",
                TestCaseID = "TC-PAY-CRE-02",
                Description = "Tạo payment với VipPackage không hoạt động (IsActive = false)",
                ExpectedResult = "Return 400 VipPackageInactive",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "VipPackage tồn tại",
                    "IsActive = false",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPackage_ShouldCreatePaymentAndReturn201()
        {
            // Arrange
            var command = new CreatePaymentCommand
            {
                UserId = "USER-001",
                VipPackageId = "PKG-001"
            };

            var activePackage = MockVipPackageRepository.GetSamplePackage();

            Payment? capturedPayment = null;
            var mockPaymentRepo = new Mock<IPaymentRepository>();
            mockPaymentRepo.Setup(x => x.AddAsync(It.IsAny<Payment>()))
                           .Callback<Payment>(p => capturedPayment = p)
                           .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                paymentRepo: mockPaymentRepo,
                vipRepo: MockVipPackageRepository.GetMock(
                    returnedPackage: activePackage));

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.PaymentId.Should().NotBeNullOrEmpty();
            result.Data.PaymentUrl.Should().Contain("sepay.vn");

            capturedPayment.Should().NotBeNull();
            capturedPayment!.UserId.Should().Be("USER-001");
            capturedPayment.Amount.Should().Be(activePackage.Price);
            capturedPayment.Status.Should().Be(Domain.Enums.PaymentStatus.Pending);

            QACollector.LogTestCase("Payment - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Payment",
                TestCaseID = "TC-PAY-CRE-03",
                Description = "Tạo payment với VipPackage Active hợp lệ → trả về PaymentId và QR URL",
                ExpectedResult = "Return 201, PaymentId != null, PaymentUrl chứa 'sepay.vn', Status = Pending",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VipPackageId",
                    "IsActive = true",
                    "AddAsync called",
                    "Status = Pending",
                    "Return 201"
                }
            });
        }
    }
}