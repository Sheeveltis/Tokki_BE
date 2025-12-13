using FluentAssertions;
using Moq;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Statistics.Queries
{
    public class GetDashboardOverviewHandlerTests : StatisticsTestBase
    {
        private readonly GetDashboardOverviewHandler _handler;

        public GetDashboardOverviewHandlerTests()
        {
            _handler = new GetDashboardOverviewHandler(_mockStatsRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnData_When_Called()
        {
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;
            var expectedData = StatisticsTestData.GetDashboardOverview();

            _mockStatsRepo.Setup(x => x.GetOverviewAsync(startDate, endDate))
                          .ReturnsAsync(expectedData);

            var query = new GetDashboardOverviewQuery { StartDate = startDate, EndDate = endDate };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(expectedData);
            result.Data.TotalRevenue.Should().Be(15000000);

            _mockStatsRepo.Verify(x => x.GetOverviewAsync(startDate, endDate), Times.Once);
        }
    }
}