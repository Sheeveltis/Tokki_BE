using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Titles.Queries.GetAllTitles;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles
{
    public class GetAllTitlesQueryHandlerTests
    {
        private static Mock<ITitleRepository> GetRepoMock(List<Title>? titles = null)
        {
            var m = new Mock<ITitleRepository>();
            m.Setup(x => x.GetAllTitlesAsync(It.IsAny<bool>()))
             .ReturnsAsync(titles ?? new List<Title>());
            return m;
        }

        private static GetAllTitlesQueryHandler CreateHandler(Mock<ITitleRepository>? repo = null)
            => new GetAllTitlesQueryHandler((repo ?? GetRepoMock()).Object);

        private static List<Title> SampleTitles() => new List<Title>
        {
            new Title { TitleId = "T-001", Name = "Bậc học giả",  RequiredXP = 1000 },
            new Title { TitleId = "T-002", Name = "Bậc chuyên gia", RequiredXP = 5000 },
            new Title { TitleId = "T-003", Name = "Bậc tiến sĩ",  RequiredXP = 10000 }
        };

        // TC-TITLE-GALL-01 | N | Happy path: 3 titles returned → 200 with Count=3
        [Fact]
        public async Task Handle_RepoReturns3Titles_ShouldReturn200With3Items()
        {
            var repo   = GetRepoMock(SampleTitles());
            var result = await CreateHandler(repo).Handle(new GetAllTitlesQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(3);
            QACollector.LogTestCase("Title - Get All", new TestCaseDetail { FunctionGroup = "GetAllTitles", TestCaseID = "TC-TITLE-GALL-01", Description = "3 titles → 200, Count=3", ExpectedResult = "IsSuccess=true, 200, Data.Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetAllTitlesAsync returns 3 items" } });
        }

        // TC-TITLE-GALL-02 | N | Empty list → 200 with empty list
        [Fact]
        public async Task Handle_NoTitles_ShouldReturn200WithEmptyList()
        {
            var result = await CreateHandler(GetRepoMock(new List<Title>())).Handle(new GetAllTitlesQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            QACollector.LogTestCase("Title - Get All", new TestCaseDetail { FunctionGroup = "GetAllTitles", TestCaseID = "TC-TITLE-GALL-02", Description = "No titles → 200 with empty list", ExpectedResult = "IsSuccess=true, Data=[]", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No titles in system" } });
        }

        // TC-TITLE-GALL-03 | B | GetAllTitlesAsync called exactly once
        [Fact]
        public async Task Handle_CallsGetAllTitlesAsync_ExactlyOnce()
        {
            var repo = GetRepoMock(SampleTitles());
            await CreateHandler(repo).Handle(new GetAllTitlesQuery(), CancellationToken.None);
            repo.Verify(x => x.GetAllTitlesAsync(It.IsAny<bool>()), Times.Once);
            QACollector.LogTestCase("Title - Get All", new TestCaseDetail { FunctionGroup = "GetAllTitles", TestCaseID = "TC-TITLE-GALL-03", Description = "GetAllTitlesAsync called exactly once", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Single repo call" } });
        }

        // TC-TITLE-GALL-04 | N | First title fields correctly returned (TitleId, Name, RequiredXP)
        [Fact]
        public async Task Handle_ReturnsData_FirstTitleFieldsCorrect()
        {
            var repo   = GetRepoMock(SampleTitles());
            var result = await CreateHandler(repo).Handle(new GetAllTitlesQuery(), CancellationToken.None);
            result.Data![0].TitleId.Should().Be("T-001");
            result.Data[0].Name.Should().Be("Bậc học giả");
            result.Data[0].RequiredXP.Should().Be(1000);
            QACollector.LogTestCase("Title - Get All", new TestCaseDetail { FunctionGroup = "GetAllTitles", TestCaseID = "TC-TITLE-GALL-04", Description = "First title: TitleId='T-001', Name='Bậc học giả', XP=1000", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Title entity returned directly" } });
        }

        // TC-TITLE-GALL-05 | N | Data is same reference list as returned by repo
        [Fact]
        public async Task Handle_ReturnedList_IsSameReferenceAsRepo()
        {
            var list   = SampleTitles();
            var result = await CreateHandler(GetRepoMock(list)).Handle(new GetAllTitlesQuery(), CancellationToken.None);
            result.Data.Should().BeSameAs(list);
            QACollector.LogTestCase("Title - Get All", new TestCaseDetail { FunctionGroup = "GetAllTitles", TestCaseID = "TC-TITLE-GALL-05", Description = "Result.Data is same reference as repo list (no cloning)", ExpectedResult = "Same reference", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Handler passes repo list directly" } });
        }

        // TC-TITLE-GALL-06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<ITitleRepository>();
            repo.Setup(x => x.GetAllTitlesAsync(It.IsAny<bool>())).ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(new GetAllTitlesQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("Title - Get All", new TestCaseDetail { FunctionGroup = "GetAllTitles", TestCaseID = "TC-TITLE-GALL-06", Description = "Repository throws → exception propagates (no try-catch)", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No exception handling in handler" } });
        }
    }
}
