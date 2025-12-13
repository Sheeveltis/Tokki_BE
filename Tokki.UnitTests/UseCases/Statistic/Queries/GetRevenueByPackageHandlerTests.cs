using FluentAssertions;
using Moq;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Tokki.UnitTests.UseCases.Statistics.Queries.GetRevenueByPackage; 
using Xunit;

namespace Tokki.UnitTests.Features.Statistics.Queries
{
    public class GetRevenueByPackageHandlerTests : StatisticsTestBase
    {
        private readonly GetRevenueByPackageHandler _handler;

        public GetRevenueByPackageHandlerTests()
        {
            _handler = new GetRevenueByPackageHandler(_mockStatsRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnList_When_Called()
        {
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now;
            var expectedData = StatisticsTestData.GetRevenueByPackages();

            _mockStatsRepo.Setup(x => x.GetRevenueByPackageAsync(startDate, endDate))
                          .ReturnsAsync(expectedData);

            var query = new GetRevenueByPackageQuery { StartDate = startDate, EndDate = endDate };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data[0].PackageName.Should().Be("VIP 1 Tháng");

            _mockStatsRepo.Verify(x => x.GetRevenueByPackageAsync(startDate, endDate), Times.Once);
        }
    }
}