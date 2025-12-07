using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Domain.Entities;
using Xunit;

namespace Tokki.UnitTests.Application.UseCases.Payments.Commands.CreatePayment
{
    public class CreatePaymentCommandHandlerTests
    {
        private readonly Mock<IPaymentRepository> _mockPaymentRepository;
        private readonly Mock<ISePayService> _mockSePayService;
        private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;

        private readonly CreatePaymentCommandHandler _handler;

        public CreatePaymentCommandHandlerTests()
        {
            _mockPaymentRepository = new Mock<IPaymentRepository>();
            _mockSePayService = new Mock<ISePayService>();
            _mockIdGeneratorService = new Mock<IIdGeneratorService>();

            _mockIdGeneratorService.Setup(x => x.GenerateCustom(It.IsAny<int>()))
                                   .Returns("MOCK_ID_10");

            _mockSePayService.Setup(x => x.GenerateQrUrl(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                             .Returns("https://sepay.fake/qr-code");

            _handler = new CreatePaymentCommandHandler(
                _mockPaymentRepository.Object,
                _mockSePayService.Object,
                _mockIdGeneratorService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            var command = new CreatePaymentCommand
            {
                Amount = 50000,
                Description = "Thanh toan test",
                UserId = "User_01"
            };

            var result = await _handler.Handle(command, CancellationToken.None);


            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201); 
            result.Message.Should().NotBeNullOrEmpty();

            result.Data.Should().NotBeNull();
            result.Data.PaymentId.Should().Be("MOCK_ID_10"); 
            result.Data.PaymentUrl.Should().Be("https://sepay.fake/qr-code");

            
            _mockPaymentRepository.Verify(
                x => x.AddAsync(It.Is<Payment>(p =>
                    p.Id == "MOCK_ID_10" &&
                    p.Amount == 50000 &&
                    p.Status == Tokki.Domain.Enums.PaymentStatus.Pending
                )),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_AmountIsLessThan1000()
        {
            var command = new CreatePaymentCommand
            {
                Amount = 500, // < 1000
                UserId = "User_01"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

   
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400); 
            result.Message.Should().Contain("1,000"); 

            _mockPaymentRepository.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_RepositoryThrowsException()
        {
            var command = new CreatePaymentCommand { Amount = 50000 };

            _mockPaymentRepository
                .Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500); 

            result.Message.Should().Contain("Database connection failed");
        }
    }
}