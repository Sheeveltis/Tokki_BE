using FluentAssertions;
using Moq;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Tokki.UnitTests.UseCases.Statistics.Queries.GetRevenueChart;

namespace Tokki.UnitTests.Features.Statistics.Queries
{
    public class GetRevenueChartHandlerTests : StatisticsTestBase
    {
        private readonly GetRevenueChartHandler _handler;

        public GetRevenueChartHandlerTests()
        {
            _handler = new GetRevenueChartHandler(_mockStatsRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnChartData_When_Called()
        {
            var year = 2023;
            var expectedData = StatisticsTestData.GetRevenueChart(); 

            _mockStatsRepo.Setup(x => x.GetRevenueChartAsync(year))
                          .ReturnsAsync(expectedData);

            var query = new GetRevenueChartQuery { Year = year };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2); 

            result.Data[0].Month.Should().Be("Tháng 1");
            result.Data[0].Revenue.Should().Be(500000);
            result.Data[0].TotalOrders.Should().Be(50);

            _mockStatsRepo.Verify(x => x.GetRevenueChartAsync(year), Times.Once);
        }
    }
}