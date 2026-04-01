using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticBlog.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.StatisticBlog
{
    public class GetDashboardStatsQueryHandlerTests
    {
        private static Mock<IStatisticBlogRepository> GetRepoMock(DashboardStatDTO? data = null)
        {
            var m = new Mock<IStatisticBlogRepository>();
            m.Setup(x => x.GetDashboardStatsAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(data ?? new DashboardStatDTO());
            return m;
        }

        private static GetDashboardStatsQueryHandler CreateHandler(Mock<IStatisticBlogRepository>? repo = null)
            => new GetDashboardStatsQueryHandler((repo ?? GetRepoMock()).Object);

        // TC-SB-DS-01 | N | Happy path: repository returns stats → 200 success
        [Fact]
        public async Task Handle_RepoReturnsStats_ShouldReturn200()
        {
            var stats = new DashboardStatDTO { TotalBlogs = 10, TotalPublished = 3, TotalViews = 500 };
            var repo   = GetRepoMock(stats);
            var result = await CreateHandler(repo).Handle(new GetDashboardStatsQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            QACollector.LogTestCase("StatisticBlog - Dashboard Stats", new TestCaseDetail { FunctionGroup = "GetDashboardStats", TestCaseID = "TC-SB-DS-01", Description = "Happy path: repo returns stats → 200", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetDashboardStatsAsync returns populated DTO" } });
        }

        // TC-SB-DS-02 | N | TotalBlogs, TotalPublished, TotalViews mapped correctly
        [Fact]
        public async Task Handle_RepoReturnsStats_FieldsMappedCorrectly()
        {
            var stats = new DashboardStatDTO { TotalBlogs = 42, TotalPublished = 7, TotalViews = 1234 };
            var result = await CreateHandler(GetRepoMock(stats)).Handle(new GetDashboardStatsQuery(), CancellationToken.None);
            result.Data!.TotalBlogs.Should().Be(42);
            result.Data.TotalPublished.Should().Be(7);
            result.Data.TotalViews.Should().Be(1234);
            QACollector.LogTestCase("StatisticBlog - Dashboard Stats", new TestCaseDetail { FunctionGroup = "GetDashboardStats", TestCaseID = "TC-SB-DS-02", Description = "DTO fields TotalBlogs=42, TotalPublished=7, TotalViews=1234 mapped correctly", ExpectedResult = "All fields verified", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Stats fields passed through correctly" } });
        }

        // TC-SB-DS-03 | B | GetDashboardStatsAsync called exactly once
        [Fact]
        public async Task Handle_ValidRequest_GetDashboardStatsCalledOnce()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(new GetDashboardStatsQuery(), CancellationToken.None);
            repo.Verify(x => x.GetDashboardStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("StatisticBlog - Dashboard Stats", new TestCaseDetail { FunctionGroup = "GetDashboardStats", TestCaseID = "TC-SB-DS-03", Description = "GetDashboardStatsAsync called exactly once", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Repository called once per request" } });
        }

        // TC-SB-DS-04 | N | Zeroed stats DTO → still returns 200 (empty dashboard)
        [Fact]
        public async Task Handle_ZeroStats_ShouldReturn200WithZeroes()
        {
            var stats  = new DashboardStatDTO { TotalBlogs = 0, TotalPublished = 0, TotalViews = 0 };
            var result = await CreateHandler(GetRepoMock(stats)).Handle(new GetDashboardStatsQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalBlogs.Should().Be(0);
            QACollector.LogTestCase("StatisticBlog - Dashboard Stats", new TestCaseDetail { FunctionGroup = "GetDashboardStats", TestCaseID = "TC-SB-DS-04", Description = "Zeroed stats DTO → still 200 success (empty dashboard allowed)", ExpectedResult = "IsSuccess=true, TotalBlogs=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All stats = 0", "no blogs yet" } });
        }

        // TC-SB-DS-05 | A | Repository throws exception → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<IStatisticBlogRepository>();
            repo.Setup(x => x.GetDashboardStatsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));
            var act = async () => await CreateHandler(repo).Handle(new GetDashboardStatsQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
            QACollector.LogTestCase("StatisticBlog - Dashboard Stats", new TestCaseDetail { FunctionGroup = "GetDashboardStats", TestCaseID = "TC-SB-DS-05", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown with 'Database error'", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetDashboardStatsAsync throws" } });
        }

        // TC-SB-DS-06 | N | Result data reference equals the object returned by repo
        [Fact]
        public async Task Handle_RepoReturnsStats_DataReferenceIsTheSameObject()
        {
            var stats  = new DashboardStatDTO { TotalBlogs = 5 };
            var result = await CreateHandler(GetRepoMock(stats)).Handle(new GetDashboardStatsQuery(), CancellationToken.None);
            result.Data.Should().BeSameAs(stats);
            QACollector.LogTestCase("StatisticBlog - Dashboard Stats", new TestCaseDetail { FunctionGroup = "GetDashboardStats", TestCaseID = "TC-SB-DS-06", Description = "Result.Data is the exact object returned by repository (no cloning)", ExpectedResult = "Data is same reference", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Handler passes through repo result directly" } });
        }
    }
}
