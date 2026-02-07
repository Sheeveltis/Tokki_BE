using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.VipPackages.Commands.DeleteVipPackage;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.VipPackages.Commands
{
    public class DeleteVipPackageHandlerTests : VipPackageTestBase
    {
        private readonly DeleteVipPackageHandler _handler;

        public DeleteVipPackageHandlerTests()
        {
            _handler = new DeleteVipPackageHandler(_mockVipRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_PackageNotFound()
        {
            var command = new DeleteVipPackageCommand { Id = "VIP_UNKNOWN" };
            _mockVipRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync((VipPackage?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_SoftDelete_When_PackageExists()
        {
            var command = new DeleteVipPackageCommand { Id = "VIP_01" };
            var package = VipPackageTestData.GetVipPackage("VIP_01", isActive: true);

            _mockVipRepo.Setup(x => x.GetByIdAsync(command.Id)).ReturnsAsync(package);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            package.IsActive.Should().BeFalse();

            _mockVipRepo.Verify(x => x.UpdateAsync(package), Times.Once);
        }
    }
}