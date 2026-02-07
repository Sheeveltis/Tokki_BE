using FluentAssertions;
using Moq;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Tokki.UnitTests.UseCases.Statistics.Queries.GetTransactionsReport;
using Xunit;

namespace Tokki.UnitTests.Features.Statistics.Queries
{
    public class GetTransactionsReportHandlerTests : StatisticsTestBase
    {
        private readonly GetTransactionsReportHandler _handler;

        public GetTransactionsReportHandlerTests()
        {
            _handler = new GetTransactionsReportHandler(_mockStatsRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedResult_When_Called()
        {
            var query = new GetTransactionsReportQuery
            {
                Search = "Nguyen Van A",
                Status = "Paid",
                Page = 1,
                PageSize = 10,
                FromDate = DateTime.Now.AddDays(-10),
                ToDate = DateTime.Now
            };

            var items = StatisticsTestData.GetTransactions();
            var totalCount = 50;

            _mockStatsRepo.Setup(x => x.GetTransactionsAsync(
                query.Search,
                query.Status,
                query.FromDate,
                query.ToDate,
                query.Page,
                query.PageSize
            )).ReturnsAsync((items, totalCount));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(totalCount);
            result.Data.PageSize.Should().Be(10);
            result.Data.PageNumber.Should().Be(1);

            _mockStatsRepo.Verify(x => x.GetTransactionsAsync(
                query.Search, query.Status, query.FromDate, query.ToDate, query.Page, query.PageSize
            ), Times.Once);
        }
    }
}