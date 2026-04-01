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
    public class GetTopAuthorsQueryHandlerTests
    {
        private static List<TopAuthorDTO> SampleAuthors(int count = 3) =>
            new List<TopAuthorDTO> { new TopAuthorDTO { AuthorId = "A1", TotalViews = 1000, BlogCount = 10 }, new TopAuthorDTO { AuthorId = "A2", TotalViews = 800, BlogCount = 8 }, new TopAuthorDTO { AuthorId = "A3", TotalViews = 500, BlogCount = 5 } }.GetRange(0, count);

        private static Mock<IStatisticBlogRepository> GetRepoMock(List<TopAuthorDTO>? data = null)
        {
            var m = new Mock<IStatisticBlogRepository>();
            m.Setup(x => x.GetTopAuthorsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(data ?? new List<TopAuthorDTO>());
            return m;
        }

        private static GetTopAuthorsQueryHandler CreateHandler(Mock<IStatisticBlogRepository>? repo = null)
            => new GetTopAuthorsQueryHandler((repo ?? GetRepoMock()).Object);

        // TC-SB-TA-01 | N | Happy path: repo returns 3 authors → 200 with 3 items
        [Fact]
        public async Task Handle_RepoReturnsAuthors_ShouldReturn200WithList()
        {
            var authors = SampleAuthors(3);
            var repo    = GetRepoMock(authors);
            var result  = await CreateHandler(repo).Handle(new GetTopAuthorsQuery { Count = 3 }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(3);
            QACollector.LogTestCase("StatisticBlog - Top Authors", new TestCaseDetail { FunctionGroup = "GetTopAuthors", TestCaseID = "TC-SB-TA-01", Description = "Happy path: 3 authors returned → 200, Count=3", ExpectedResult = "IsSuccess=true, Data.Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTopAuthorsAsync returns 3 items" } });
        }

        // TC-SB-TA-02 | N | Author fields mapped correctly (AuthorId, TotalViews, BlogCount)
        [Fact]
        public async Task Handle_RepoReturnsAuthors_FieldsMappedCorrectly()
        {
            var authors = new List<TopAuthorDTO> { new TopAuthorDTO { AuthorId = "A99", TotalViews = 2500, BlogCount = 25 } };
            var result  = await CreateHandler(GetRepoMock(authors)).Handle(new GetTopAuthorsQuery { Count = 1 }, CancellationToken.None);
            result.Data![0].AuthorId.Should().Be("A99");
            result.Data[0].BlogCount.Should().Be(25);
            result.Data[0].TotalViews.Should().Be(2500);
            QACollector.LogTestCase("StatisticBlog - Top Authors", new TestCaseDetail { FunctionGroup = "GetTopAuthors", TestCaseID = "TC-SB-TA-02", Description = "Author fields mapped correctly", ExpectedResult = "AuthorId='A99', BlogCount=25", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Author fields passed directly" } });
        }

        // TC-SB-TA-03 | B | GetTopAuthorsAsync called with correct Count parameter
        [Fact]
        public async Task Handle_WithCount5_GetTopAuthorsCalledWithCount5()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(new GetTopAuthorsQuery { Count = 5 }, CancellationToken.None);
            repo.Verify(x => x.GetTopAuthorsAsync(5, It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("StatisticBlog - Top Authors", new TestCaseDetail { FunctionGroup = "GetTopAuthors", TestCaseID = "TC-SB-TA-03", Description = "GetTopAuthorsAsync called with Count=5", ExpectedResult = "Times.Once with count=5", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Count=5 passed correctly to repo" } });
        }

        // TC-SB-TA-04 | B | Default Count=5 used when not specified
        [Fact]
        public async Task Handle_DefaultCount_ShouldCallRepoWithFive()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(new GetTopAuthorsQuery(), CancellationToken.None); // default Count=5
            repo.Verify(x => x.GetTopAuthorsAsync(5, It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("StatisticBlog - Top Authors", new TestCaseDetail { FunctionGroup = "GetTopAuthors", TestCaseID = "TC-SB-TA-04", Description = "Default Count=5 → repo called with 5", ExpectedResult = "GetTopAuthorsAsync(5, ...) Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTopAuthorsQuery().Count = 5 by default" } });
        }

        // TC-SB-TA-05 | N | Empty list → 200 with empty data
        [Fact]
        public async Task Handle_NoAuthors_ShouldReturn200WithEmptyList()
        {
            var result = await CreateHandler(GetRepoMock(new List<TopAuthorDTO>())).Handle(new GetTopAuthorsQuery { Count = 5 }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            QACollector.LogTestCase("StatisticBlog - Top Authors", new TestCaseDetail { FunctionGroup = "GetTopAuthors", TestCaseID = "TC-SB-TA-05", Description = "No authors in system → 200 with empty list", ExpectedResult = "IsSuccess=true, Data=[]", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Repo returns empty list", "no authors yet" } });
        }

        // TC-SB-TA-06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<IStatisticBlogRepository>();
            repo.Setup(x => x.GetTopAuthorsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(new GetTopAuthorsQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("StatisticBlog - Top Authors", new TestCaseDetail { FunctionGroup = "GetTopAuthors", TestCaseID = "TC-SB-TA-06", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTopAuthorsAsync throws" } });
        }
    }
}
