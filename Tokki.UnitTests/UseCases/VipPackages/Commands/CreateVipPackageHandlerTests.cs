using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.VipPackages.Commands.CreateVipPackage;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.VipPackages.Commands
{
    public class CreateVipPackageHandlerTests : VipPackageTestBase
    {
        private readonly CreateVipPackageHandler _handler;

        public CreateVipPackageHandlerTests()
        {
            _handler = new CreateVipPackageHandler(_mockVipRepo.Object, _mockIdGen.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            var command = VipPackageTestData.GetCreateCommand();

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("VIP_MOCK_ID");

            _mockVipRepo.Verify(x => x.AddAsync(It.Is<VipPackage>(p =>
                p.Id == "VIP_MOCK_ID" &&
                p.Name == command.Name &&
                p.Price == command.Price &&
                p.IsActive == false 
            )), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_PriceIsInvalid()
        {
            var command = VipPackageTestData.GetCreateCommand();
            command.Price = -1000;

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageInvalidPrice.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DurationIsInvalid()
        {
            var command = VipPackageTestData.GetCreateCommand();
            command.DurationDays = 0;

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageInvalidDuration.Code);
        }
    }
}