using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Titles.Queries.GetTitleById;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles
{
    public class GetTitleByIdQueryHandlerTests
    {
        private static Mock<ITitleRepository> GetRepoMock(Title? title = null)
        {
            var m = new Mock<ITitleRepository>();
            m.Setup(x => x.GetTitleByIdAsync(It.IsAny<string>())).ReturnsAsync(title);
            return m;
        }

        private static GetTitleByIdQueryHandler CreateHandler(Mock<ITitleRepository>? repo = null)
            => new GetTitleByIdQueryHandler((repo ?? GetRepoMock()).Object);

        private static Title SampleTitle(string id = "T-001") => new Title
        {
            TitleId     = id,
            Name        = "Bậc học giả",
            Description = "Top learner",
            RequirementQuantity = 1000,
            ColorHex    = "#GOLD"
        };

        // GetTitleById_01 | A | Title not found → 404 failure
        [Fact]
        public async Task Handle_TitleNotFound_ShouldReturn404Failure()
        {
            var result = await CreateHandler(GetRepoMock(null)).Handle(new GetTitleByIdQuery("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Title - Get By Id", new TestCaseDetail { FunctionGroup = "GetTitleById", TestCaseID = "GetTitleById_01", Description = "Title not found → 404 failure", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTitleByIdAsync returns null" } });
        }

        // GetTitleById_02 | N | Happy path: title found → 200 with Title entity
        [Fact]
        public async Task Handle_TitleFound_ShouldReturn200WithTitle()
        {
            var result = await CreateHandler(GetRepoMock(SampleTitle("T-001"))).Handle(new GetTitleByIdQuery("T-001"), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.TitleId.Should().Be("T-001");
            QACollector.LogTestCase("Title - Get By Id", new TestCaseDetail { FunctionGroup = "GetTitleById", TestCaseID = "GetTitleById_02", Description = "Title found → 200, Data.TitleId='T-001'", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTitleByIdAsync returns title" } });
        }

        // GetTitleById_03 | N | All title fields present in result
        [Fact]
        public async Task Handle_TitleFound_AllFieldsPresent()
        {
            var result = await CreateHandler(GetRepoMock(SampleTitle())).Handle(new GetTitleByIdQuery("T-001"), CancellationToken.None);
            result.Data!.Name.Should().Be("Bậc học giả");
            result.Data.RequirementQuantity.Should().Be(1000);
            result.Data.ColorHex.Should().Be("#GOLD");
            QACollector.LogTestCase("Title - Get By Id", new TestCaseDetail { FunctionGroup = "GetTitleById", TestCaseID = "GetTitleById_03", Description = "All fields (Name, XP, IsSystemGiven, ColorHex) present", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Title entity returned directly" } });
        }

        // GetTitleById_04 | B | GetTitleByIdAsync called with correct ID
        [Fact]
        public async Task Handle_WithId_GetByIdCalledWithCorrectId()
        {
            var repo = GetRepoMock(SampleTitle("T-XYZ"));
            await CreateHandler(repo).Handle(new GetTitleByIdQuery("T-XYZ"), CancellationToken.None);
            repo.Verify(x => x.GetTitleByIdAsync("T-XYZ"), Times.Once);
            QACollector.LogTestCase("Title - Get By Id", new TestCaseDetail { FunctionGroup = "GetTitleById", TestCaseID = "GetTitleById_04", Description = "GetTitleByIdAsync called with 'T-XYZ'", ExpectedResult = "Times.Once with correct ID", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "ID forwarded to repo" } });
        }

        // GetTitleById_05 | N | Data is same reference as returned by repo
        [Fact]
        public async Task Handle_TitleFound_DataIsSameReference()
        {
            var title  = SampleTitle();
            var result = await CreateHandler(GetRepoMock(title)).Handle(new GetTitleByIdQuery("T-001"), CancellationToken.None);
            result.Data.Should().BeSameAs(title);
            QACollector.LogTestCase("Title - Get By Id", new TestCaseDetail { FunctionGroup = "GetTitleById", TestCaseID = "GetTitleById_05", Description = "Result.Data is same reference as repo entity (no cloning)", ExpectedResult = "Same reference", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Handler returns repo entity directly" } });
        }

        // GetTitleById_06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<ITitleRepository>();
            repo.Setup(x => x.GetTitleByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(new GetTitleByIdQuery("T-001"), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("Title - Get By Id", new TestCaseDetail { FunctionGroup = "GetTitleById", TestCaseID = "GetTitleById_06", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No exception handling in handler" } });
        }
    }
}
