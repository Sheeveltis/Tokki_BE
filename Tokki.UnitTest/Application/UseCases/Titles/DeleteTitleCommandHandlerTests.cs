using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Titles.Commands.DeleteTitle;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles
{
    public class DeleteTitleCommandHandlerTests
    {
        private static Mock<ITitleRepository> GetRepoMock(Title? title = null)
        {
            var m = new Mock<ITitleRepository>();
            m.Setup(x => x.GetTitleByIdAsync(It.IsAny<string>())).ReturnsAsync(title);
            m.Setup(x => x.UpdateAsync(It.IsAny<Title>())).Returns(Task.CompletedTask);
            return m;
        }

        private static DeleteTitleCommandHandler CreateHandler(Mock<ITitleRepository>? repo = null)
            => new DeleteTitleCommandHandler((repo ?? GetRepoMock()).Object);

        private static Title SampleTitle(string id = "T-001") => new Title
        {
            TitleId    = id,
            Name       = "Bậc học giả",
            RequiredXP = 1000,
            Status     = TitleStatus.Active
        };

        // TC-TITLE-DEL-01 | A | Title not found → 404 failure
        [Fact]
        public async Task Handle_TitleNotFound_ShouldReturn404Failure()
        {
            var repo   = GetRepoMock(null);
            var result = await CreateHandler(repo).Handle(new DeleteTitleCommand("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Title - Delete", new TestCaseDetail { FunctionGroup = "DeleteTitle", TestCaseID = "TC-TITLE-DEL-01", Description = "Title not found → 404 failure", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTitleByIdAsync returns null" } });
        }

        // TC-TITLE-DEL-02 | N | Happy path: title found → soft-deleted (Status=Inactive), 200
        [Fact]
        public async Task Handle_TitleFound_ShouldSetStatusInactiveAndReturn200()
        {
            var title  = SampleTitle();
            var repo   = GetRepoMock(title);
            var result = await CreateHandler(repo).Handle(new DeleteTitleCommand("T-001"), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            title.Status.Should().Be(TitleStatus.Inactive);
            QACollector.LogTestCase("Title - Delete", new TestCaseDetail { FunctionGroup = "DeleteTitle", TestCaseID = "TC-TITLE-DEL-02", Description = "Title found → Status=Inactive (soft delete), 200", ExpectedResult = "IsSuccess=true, 200, Status=Inactive", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Soft delete via Status flag" } });
        }

        // TC-TITLE-DEL-03 | B | UpdateAsync called once on success (soft delete)
        [Fact]
        public async Task Handle_TitleFound_UpdateCalledOnce()
        {
            var repo = GetRepoMock(SampleTitle());
            await CreateHandler(repo).Handle(new DeleteTitleCommand("T-001"), CancellationToken.None);
            repo.Verify(x => x.UpdateAsync(It.IsAny<Title>()), Times.Once);
            QACollector.LogTestCase("Title - Delete", new TestCaseDetail { FunctionGroup = "DeleteTitle", TestCaseID = "TC-TITLE-DEL-03", Description = "UpdateAsync called once (soft delete commits to DB)", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Soft delete persisted" } });
        }

        // TC-TITLE-DEL-04 | B | Title not found → UpdateAsync never called
        [Fact]
        public async Task Handle_TitleNotFound_UpdateNeverCalled()
        {
            var repo = GetRepoMock(null);
            await CreateHandler(repo).Handle(new DeleteTitleCommand("MISSING"), CancellationToken.None);
            repo.Verify(x => x.UpdateAsync(It.IsAny<Title>()), Times.Never);
            QACollector.LogTestCase("Title - Delete", new TestCaseDetail { FunctionGroup = "DeleteTitle", TestCaseID = "TC-TITLE-DEL-04", Description = "Title not found → early return, UpdateAsync never called", ExpectedResult = "Times.Never", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Guard returns before update" } });
        }

        // TC-TITLE-DEL-05 | N | Already inactive title → still soft-deleted again (idempotent)
        [Fact]
        public async Task Handle_AlreadyInactiveTitle_ShouldReturn200AndRemainInactive()
        {
            var title = SampleTitle();
            title.Status = TitleStatus.Inactive;
            var result = await CreateHandler(GetRepoMock(title)).Handle(new DeleteTitleCommand("T-001"), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            title.Status.Should().Be(TitleStatus.Inactive);
            QACollector.LogTestCase("Title - Delete", new TestCaseDetail { FunctionGroup = "DeleteTitle", TestCaseID = "TC-TITLE-DEL-05", Description = "Already inactive title → still 200 (idempotent operation)", ExpectedResult = "IsSuccess=true, Status still Inactive", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Title already Inactive, handler doesn't check" } });
        }

        // TC-TITLE-DEL-06 | N | GetTitleByIdAsync called with correct ID
        [Fact]
        public async Task Handle_WithId_GetByIdCalledWithCorrectId()
        {
            var repo = GetRepoMock(SampleTitle("T-XYZ"));
            await CreateHandler(repo).Handle(new DeleteTitleCommand("T-XYZ"), CancellationToken.None);
            repo.Verify(x => x.GetTitleByIdAsync("T-XYZ"), Times.Once);
            QACollector.LogTestCase("Title - Delete", new TestCaseDetail { FunctionGroup = "DeleteTitle", TestCaseID = "TC-TITLE-DEL-06", Description = "GetTitleByIdAsync called with 'T-XYZ'", ExpectedResult = "Times.Once with correct ID", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "ID forwarded to repo" } });
        }
    }
}
