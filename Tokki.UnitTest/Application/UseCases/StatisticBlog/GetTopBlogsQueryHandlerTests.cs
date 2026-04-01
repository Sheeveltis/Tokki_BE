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
    public class GetTopBlogsQueryHandlerTests
    {
        private static Mock<IStatisticBlogRepository> GetRepoMock(List<TopBlogDTO>? data = null)
        {
            var m = new Mock<IStatisticBlogRepository>();
            m.Setup(x => x.GetTopViewedBlogsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(data ?? new List<TopBlogDTO>());
            return m;
        }

        private static GetTopBlogsQueryHandler CreateHandler(Mock<IStatisticBlogRepository>? repo = null)
            => new GetTopBlogsQueryHandler((repo ?? GetRepoMock()).Object);

        private static List<TopBlogDTO> SampleBlogs() => new List<TopBlogDTO>
        {
            new TopBlogDTO { Id = "B1", Title = "Học tiếng Hàn", ViewCount = 500 },
            new TopBlogDTO { Id = "B2", Title = "TOPIK Tips",    ViewCount = 300 }
        };

        // TC-SB-TB-01 | N | Happy path: 2 blogs returned → 200 with Count=2
        [Fact]
        public async Task Handle_RepoReturnsBlogs_ShouldReturn200WithList()
        {
            var result = await CreateHandler(GetRepoMock(SampleBlogs())).Handle(new GetTopBlogsQuery { Count = 2 }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(2);
            QACollector.LogTestCase("StatisticBlog - Top Blogs", new TestCaseDetail { FunctionGroup = "GetTopBlogs", TestCaseID = "TC-SB-TB-01", Description = "2 blogs returned → 200, Count=2", ExpectedResult = "IsSuccess=true, Data.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTopViewedBlogsAsync returns 2 items" } });
        }

        // TC-SB-TB-02 | N | Blog fields Title and ViewCount mapped correctly
        [Fact]
        public async Task Handle_RepoReturnsBlogs_FieldsMappedCorrectly()
        {
            var blogs  = new List<TopBlogDTO> { new TopBlogDTO { Id = "B99", Title = "Grammar Guide", ViewCount = 999 } };
            var result = await CreateHandler(GetRepoMock(blogs)).Handle(new GetTopBlogsQuery { Count = 1 }, CancellationToken.None);
            result.Data![0].Title.Should().Be("Grammar Guide");
            result.Data[0].ViewCount.Should().Be(999);
            QACollector.LogTestCase("StatisticBlog - Top Blogs", new TestCaseDetail { FunctionGroup = "GetTopBlogs", TestCaseID = "TC-SB-TB-02", Description = "Blog Title='Grammar Guide', ViewCount=999 mapped correctly", ExpectedResult = "All fields verified", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Blog fields passed directly" } });
        }

        // TC-SB-TB-03 | B | GetTopViewedBlogsAsync called with correct Count
        [Fact]
        public async Task Handle_WithCount10_GetTopViewedBlogsCalledWithCount10()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(new GetTopBlogsQuery { Count = 10 }, CancellationToken.None);
            repo.Verify(x => x.GetTopViewedBlogsAsync(10, It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("StatisticBlog - Top Blogs", new TestCaseDetail { FunctionGroup = "GetTopBlogs", TestCaseID = "TC-SB-TB-03", Description = "GetTopViewedBlogsAsync called with Count=10", ExpectedResult = "Times.Once with count=10", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Count=10 passed correctly" } });
        }

        // TC-SB-TB-04 | B | Default Count=5 when not specified
        [Fact]
        public async Task Handle_DefaultCount_ShouldCallRepoWithFive()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(new GetTopBlogsQuery(), CancellationToken.None);
            repo.Verify(x => x.GetTopViewedBlogsAsync(5, It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("StatisticBlog - Top Blogs", new TestCaseDetail { FunctionGroup = "GetTopBlogs", TestCaseID = "TC-SB-TB-04", Description = "Default Count=5 → repo called with 5", ExpectedResult = "GetTopViewedBlogsAsync(5, ...) Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTopBlogsQuery().Count=5 by default" } });
        }

        // TC-SB-TB-05 | N | Empty list → 200 with empty data
        [Fact]
        public async Task Handle_NoBlogs_ShouldReturn200WithEmptyList()
        {
            var result = await CreateHandler(GetRepoMock(new List<TopBlogDTO>())).Handle(new GetTopBlogsQuery { Count = 5 }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            QACollector.LogTestCase("StatisticBlog - Top Blogs", new TestCaseDetail { FunctionGroup = "GetTopBlogs", TestCaseID = "TC-SB-TB-05", Description = "No blogs → 200 with empty list", ExpectedResult = "IsSuccess=true, Data=[]", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Repo returns [] (no blogs)", "still 200" } });
        }

        // TC-SB-TB-06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<IStatisticBlogRepository>();
            repo.Setup(x => x.GetTopViewedBlogsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(new GetTopBlogsQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("StatisticBlog - Top Blogs", new TestCaseDetail { FunctionGroup = "GetTopBlogs", TestCaseID = "TC-SB-TB-06", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTopViewedBlogsAsync throws" } });
        }
    }
}
