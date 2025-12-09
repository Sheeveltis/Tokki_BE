using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.VipPackages.Commands.UpdateVipPackage;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.VipPackages.Commands
{
    public class UpdateVipPackageHandlerTests : VipPackageTestBase
    {
        private readonly UpdateVipPackageHandler _handler;

        public UpdateVipPackageHandlerTests()
        {
            _handler = new UpdateVipPackageHandler(_mockVipRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_PackageNotFound()
        {
            var command = VipPackageTestData.GetUpdateCommand("VIP_UNKNOWN");
            _mockVipRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync((VipPackage?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_UpdateFields_When_InputIsValid()
        {
            var command = VipPackageTestData.GetUpdateCommand("VIP_01");
            var package = VipPackageTestData.GetVipPackage("VIP_01");

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync(package);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            package.Name.Should().Be("Gói VIP Updated");
            package.Price.Should().Be(60000);
            package.IsActive.Should().BeTrue();

            _mockVipRepo.Verify(x => x.UpdateAsync(package), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Ignore_StringDefaultValues()
        {
            var command = new UpdateVipPackageCommand
            {
                Id = "VIP_01",
                Name = "string", 
                Description = "string"
            };
            var package = VipPackageTestData.GetVipPackage("VIP_01");
            var originalName = package.Name;

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync(package);

            await _handler.Handle(command, CancellationToken.None);

            package.Name.Should().Be(originalName);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UpdatePriceInvalid()
        {
            var command = new UpdateVipPackageCommand { Id = "VIP_01", Price = -500 };
            var package = VipPackageTestData.GetVipPackage("VIP_01");

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync(package);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageInvalidPrice.Code);
        }
    }
}