using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Payments.Commands
{
    public class CreatePaymentCommandHandlerTests : PaymentTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_VipPackageNotFound()
        {
            var command = PaymentTestData.GetValidCreateCommand();

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.VipPackageId))
                        .ReturnsAsync((VipPackage?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageNotFound.Code);

            _mockPaymentRepo.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_VipPackageIsInactive()
        {
            var command = PaymentTestData.GetValidCreateCommand();
            var inactivePackage = PaymentTestData.GetInactiveVipPackage();
            command.VipPackageId = inactivePackage.Id;

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.VipPackageId))
                        .ReturnsAsync(inactivePackage);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageInactive.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            var command = PaymentTestData.GetValidCreateCommand();
            var vipPackage = PaymentTestData.GetActiveVipPackage();

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.VipPackageId))
                        .ReturnsAsync(vipPackage);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            result.Data.Should().NotBeNull();
            result.Data.PaymentId.Should().Be("PAY-MOCK-ID");
            result.Data.PaymentUrl.Should().Be("https://qr.sepay.vn/mock-url");

            _mockPaymentRepo.Verify(x => x.AddAsync(It.Is<Payment>(p =>
                p.Id == "PAY-MOCK-ID" &&
                p.Amount == vipPackage.Price && 
                p.VipPackageId == vipPackage.Id &&
                p.UserId == command.UserId &&
                p.Status == PaymentStatus.Pending
            )), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_DatabaseFails()
        {
           
            var command = PaymentTestData.GetValidCreateCommand();
            var vipPackage = PaymentTestData.GetActiveVipPackage();

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.VipPackageId))
                        .ReturnsAsync(vipPackage);

            _mockPaymentRepo.Setup(x => x.AddAsync(It.IsAny<Payment>()))
                            .ThrowsAsync(new Exception("DB Connection Failed"));

            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        }
    }
}