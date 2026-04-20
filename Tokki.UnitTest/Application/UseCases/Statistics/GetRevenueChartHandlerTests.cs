using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.DTOs;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTests.UseCases.Statistics.Queries.GetRevenueChart;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Statistics
{
    public class GetRevenueChartHandlerTests
    {
        private static Mock<IStatisticsRepository> GetRepoMock(List<RevenueChartDto>? data = null)
        {
            var m = new Mock<IStatisticsRepository>();
            m.Setup(x => x.GetRevenueChartAsync(It.IsAny<int>()))
             .ReturnsAsync(data ?? new List<RevenueChartDto>());
            return m;
        }

        private static GetRevenueChartHandler CreateHandler(Mock<IStatisticsRepository>? repo = null)
            => new GetRevenueChartHandler((repo ?? GetRepoMock()).Object);

        private static GetRevenueChartQuery MakeQuery(int year = 2024)
            => new GetRevenueChartQuery { Year = year };

        private static List<RevenueChartDto> Sample12Months() => new List<RevenueChartDto>
        {
            new RevenueChartDto { Month = "2024-01", Revenue = 1000m, TotalOrders = 10 },
            new RevenueChartDto { Month = "2024-02", Revenue = 1500m, TotalOrders = 15 },
            new RevenueChartDto { Month = "2024-12", Revenue = 2000m, TotalOrders = 20 }
        };

        // GetRevenueChart_01 | N | Happy path: returns monthly chart data → success
        [Fact]
        public async Task Handle_RepoReturnsList_ShouldReturnSuccessWithData()
        {
            var result = await CreateHandler(GetRepoMock(Sample12Months())).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            QACollector.LogTestCase("Statistics - Revenue Chart", new TestCaseDetail { FunctionGroup = "GetRevenueChart", TestCaseID = "GetRevenueChart_01", Description = "3 months data returned → success, Count=3", ExpectedResult = "IsSuccess=true, Data.Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetRevenueChartAsync returns 3 items" } });
        }

        // GetRevenueChart_02 | N | RevenueChartDto fields correctly mapped
        [Fact]
        public async Task Handle_ReturnsData_MonthRevenueTotalOrdersMapped()
        {
            var data   = new List<RevenueChartDto> { new RevenueChartDto { Month = "2024-05", Revenue = 7777m, TotalOrders = 42 } };
            var result = await CreateHandler(GetRepoMock(data)).Handle(MakeQuery(), CancellationToken.None);
            result.Data![0].Month.Should().Be("2024-05");
            result.Data[0].Revenue.Should().Be(7777m);
            result.Data[0].TotalOrders.Should().Be(42);
            QACollector.LogTestCase("Statistics - Revenue Chart", new TestCaseDetail { FunctionGroup = "GetRevenueChart", TestCaseID = "GetRevenueChart_02", Description = "Month='2024-05', Revenue=7777, TotalOrders=42 mapped correctly", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "DTO fields passed through" } });
        }

        // GetRevenueChart_03 | B | GetRevenueChartAsync called with correct Year
        [Fact]
        public async Task Handle_WithYear2023_RepoCalledWithYear2023()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(MakeQuery(2023), CancellationToken.None);
            repo.Verify(x => x.GetRevenueChartAsync(2023), Times.Once);
            QACollector.LogTestCase("Statistics - Revenue Chart", new TestCaseDetail { FunctionGroup = "GetRevenueChart", TestCaseID = "GetRevenueChart_03", Description = "GetRevenueChartAsync called with Year=2023", ExpectedResult = "Times.Once with year=2023", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Year parameter forwarded correctly" } });
        }

        // GetRevenueChart_04 | N | Empty list (no data for year) → 200 with empty list
        [Fact]
        public async Task Handle_NoDataForYear_ShouldReturn200WithEmptyList()
        {
            var result = await CreateHandler(GetRepoMock(new List<RevenueChartDto>())).Handle(MakeQuery(2099), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            QACollector.LogTestCase("Statistics - Revenue Chart", new TestCaseDetail { FunctionGroup = "GetRevenueChart", TestCaseID = "GetRevenueChart_04", Description = "No data for year → 200 with empty list", ExpectedResult = "IsSuccess=true, Data=[]", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Year with no records" } });
        }

        // GetRevenueChart_05 | N | Data reference from repo preserved
        [Fact]
        public async Task Handle_ReturnsData_DataIsSameRefAsRepo()
        {
            var data   = new List<RevenueChartDto> { new RevenueChartDto { Month = "2024-01", Revenue = 1m } };
            var result = await CreateHandler(GetRepoMock(data)).Handle(MakeQuery(), CancellationToken.None);
            result.Data.Should().BeSameAs(data);
            QACollector.LogTestCase("Statistics - Revenue Chart", new TestCaseDetail { FunctionGroup = "GetRevenueChart", TestCaseID = "GetRevenueChart_05", Description = "Result.Data is same reference as repo output", ExpectedResult = "Data is same reference", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Handler passes through repo list directly" } });
        }

        // GetRevenueChart_06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<IStatisticsRepository>();
            repo.Setup(x => x.GetRevenueChartAsync(It.IsAny<int>())).ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("Statistics - Revenue Chart", new TestCaseDetail { FunctionGroup = "GetRevenueChart", TestCaseID = "GetRevenueChart_06", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetRevenueChartAsync throws" } });
        }
    }
}
