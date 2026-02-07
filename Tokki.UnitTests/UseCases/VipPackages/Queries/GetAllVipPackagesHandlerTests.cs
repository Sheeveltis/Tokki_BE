using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.VipPackages.Queries.GetAllVipPackages;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.VipPackages.Queries
{
    public class GetAllVipPackagesHandlerTests : VipPackageTestBase
    {
        private readonly GetAllVipPackagesHandler _handler;

        public GetAllVipPackagesHandlerTests()
        {
            _handler = new GetAllVipPackagesHandler(_mockVipRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPackages_When_Called()
        {
            var packages = new List<VipPackage>
            {
                VipPackageTestData.GetVipPackage("VIP_1"),
                VipPackageTestData.GetVipPackage("VIP_2")
            };

            _mockVipRepo.Setup(x => x.GetAllAsync(It.IsAny<bool>()))
                        .ReturnsAsync(packages);

            var query = new GetAllVipPackagesQuery { IsAdmin = false };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().BeEquivalentTo(packages);

            _mockVipRepo.Verify(x => x.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_PassAdminFlag_Correctly()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = true };

            await _handler.Handle(query, CancellationToken.None);

            _mockVipRepo.Verify(x => x.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ExceptionOccurs()
        {
            _mockVipRepo.Setup(x => x.GetAllAsync(It.IsAny<bool>()))
                        .ThrowsAsync(new Exception("DB Error"));

            var query = new GetAllVipPackagesQuery { IsAdmin = false };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.VipPackageFetchFailed.Code);
        }
    }
}