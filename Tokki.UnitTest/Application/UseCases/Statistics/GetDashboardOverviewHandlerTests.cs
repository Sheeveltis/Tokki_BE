using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.DTOs;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Statistics
{
    public class GetDashboardOverviewHandlerTests
    {
        private static Mock<IStatisticsRepository> GetRepoMock(DashboardOverviewDto? data = null)
        {
            var m = new Mock<IStatisticsRepository>();
            m.Setup(x => x.GetOverviewAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync(data ?? new DashboardOverviewDto());
            return m;
        }

        private static GetDashboardOverviewHandler CreateHandler(Mock<IStatisticsRepository>? repo = null)
            => new GetDashboardOverviewHandler((repo ?? GetRepoMock()).Object);

        private static GetDashboardOverviewQuery MakeQuery(
            DateTime? start = null, DateTime? end = null) => new GetDashboardOverviewQuery
        {
            StartDate = start ?? new DateTime(2024, 1, 1),
            EndDate   = end   ?? new DateTime(2024, 12, 31)
        };

        // TC-STAT-DO-01 | N | Happy path: returns 200 with overview data
        [Fact]
        public async Task Handle_RepoReturnsData_ShouldReturn200()
        {
            var dto    = new DashboardOverviewDto { TotalRevenue = 5000m, TotalOrders = 100, AverageRevenue = 50m };
            var result = await CreateHandler(GetRepoMock(dto)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalRevenue.Should().Be(5000m);
            QACollector.LogTestCase("Statistics - Dashboard Overview", new TestCaseDetail { FunctionGroup = "GetDashboardOverview", TestCaseID = "TC-STAT-DO-01", Description = "Happy path: repo returns overview → success", ExpectedResult = "IsSuccess=true, TotalRevenue=5000", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetOverviewAsync returns populated DTO" } });
        }

        // TC-STAT-DO-02 | N | TotalOrders and AverageRevenue mapped correctly
        [Fact]
        public async Task Handle_RepoReturnsData_FieldsMappedCorrectly()
        {
            var dto    = new DashboardOverviewDto { TotalRevenue = 1000m, TotalOrders = 25, AverageRevenue = 40m };
            var result = await CreateHandler(GetRepoMock(dto)).Handle(MakeQuery(), CancellationToken.None);
            result.Data!.TotalOrders.Should().Be(25);
            result.Data.AverageRevenue.Should().Be(40m);
            QACollector.LogTestCase("Statistics - Dashboard Overview", new TestCaseDetail { FunctionGroup = "GetDashboardOverview", TestCaseID = "TC-STAT-DO-02", Description = "TotalOrders=25, AverageRevenue=40 mapped correctly", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "DTO fields passed through" } });
        }

        // TC-STAT-DO-03 | B | GetOverviewAsync called with correct date range
        [Fact]
        public async Task Handle_WithDateRange_GetOverviewCalledWithCorrectDates()
        {
            var start = new DateTime(2024, 3, 1);
            var end   = new DateTime(2024, 3, 31);
            var repo  = GetRepoMock();
            await CreateHandler(repo).Handle(MakeQuery(start, end), CancellationToken.None);
            repo.Verify(x => x.GetOverviewAsync(start, end), Times.Once);
            QACollector.LogTestCase("Statistics - Dashboard Overview", new TestCaseDetail { FunctionGroup = "GetDashboardOverview", TestCaseID = "TC-STAT-DO-03", Description = "GetOverviewAsync called with exact StartDate/EndDate", ExpectedResult = "Times.Once with correct dates", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "StartDate and EndDate forwarded to repo" } });
        }

        // TC-STAT-DO-04 | N | Zeroed DTO (no revenue) → still success
        [Fact]
        public async Task Handle_ZeroRevenue_ShouldReturn200WithZeroes()
        {
            var dto    = new DashboardOverviewDto { TotalRevenue = 0m, TotalOrders = 0, AverageRevenue = 0m };
            var result = await CreateHandler(GetRepoMock(dto)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalRevenue.Should().Be(0m);
            QACollector.LogTestCase("Statistics - Dashboard Overview", new TestCaseDetail { FunctionGroup = "GetDashboardOverview", TestCaseID = "TC-STAT-DO-04", Description = "Zero revenue → still 200 (valid empty state)", ExpectedResult = "IsSuccess=true, TotalRevenue=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All fields=0, no revenue period" } });
        }

        // TC-STAT-DO-05 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<IStatisticsRepository>();
            repo.Setup(x => x.GetOverviewAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("Statistics - Dashboard Overview", new TestCaseDetail { FunctionGroup = "GetDashboardOverview", TestCaseID = "TC-STAT-DO-05", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetOverviewAsync throws" } });
        }

        // TC-STAT-DO-06 | N | Data is same reference as returned by repo
        [Fact]
        public async Task Handle_RepoReturnsDto_DataIsSameReference()
        {
            var dto    = new DashboardOverviewDto { TotalRevenue = 99m };
            var result = await CreateHandler(GetRepoMock(dto)).Handle(MakeQuery(), CancellationToken.None);
            result.Data.Should().BeSameAs(dto);
            QACollector.LogTestCase("Statistics - Dashboard Overview", new TestCaseDetail { FunctionGroup = "GetDashboardOverview", TestCaseID = "TC-STAT-DO-06", Description = "Result.Data is same reference as repo output (no cloning)", ExpectedResult = "Data is same reference", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Handler passes through repo result directly" } });
        }
    }
}
