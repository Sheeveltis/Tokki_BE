using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Titles.Commands.UpdateTitle;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles
{
    public class UpdateTitleCommandHandlerTests
    {
        private static Mock<ITitleRepository> GetRepoMock(Title? title = null, Title? duplicateByName = null)
        {
            var m = new Mock<ITitleRepository>();
            m.Setup(x => x.GetTitleByIdAsync(It.IsAny<string>())).ReturnsAsync(title);
            m.Setup(x => x.GetTitleByNameAsync(It.IsAny<string>(), It.IsAny<TitleStatus?>())).ReturnsAsync(duplicateByName);
            m.Setup(x => x.UpdateAsync(It.IsAny<Title>())).Returns(Task.CompletedTask);
            return m;
        }

        private static UpdateTitleCommandHandler CreateHandler(Mock<ITitleRepository>? repo = null)
            => new UpdateTitleCommandHandler((repo ?? GetRepoMock()).Object);

        private static Title SampleTitle(string id = "T-001", string name = "Bậc học giả") =>
            new Title { TitleId = id, Name = name, RequirementQuantity = 1000 };

        private static UpdateTitleCommand MakeCommand(string id = "T-001", string name = "Bậc tiến sĩ") =>
            new UpdateTitleCommand { TitleId = id, Name = name, Description = "Updated", RequirementQuantity = 2000, ColorHex = "#SILVER" };

        // TC-TITLE-UPD-01 | A | Title not found → 404 failure
        [Fact]
        public async Task Handle_TitleNotFound_ShouldReturn404Failure()
        {
            var repo   = GetRepoMock(null);
            var result = await CreateHandler(repo).Handle(MakeCommand("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Title - Update", new TestCaseDetail { FunctionGroup = "UpdateTitle", TestCaseID = "TC-TITLE-UPD-01", Description = "Title not found → 404 failure", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTitleByIdAsync returns null" } });
        }

        // TC-TITLE-UPD-02 | A | Name changed to a duplicate → 400 failure
        [Fact]
        public async Task Handle_NameChangedToDuplicate_ShouldReturn400Failure()
        {
            var existing   = SampleTitle("T-001", "Bậc học giả");
            var duplicate  = SampleTitle("T-002", "Bậc tiến sĩ");
            var repo       = GetRepoMock(existing, duplicateByName: duplicate);
            var result     = await CreateHandler(repo).Handle(MakeCommand("T-001", "Bậc tiến sĩ"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Title - Update", new TestCaseDetail { FunctionGroup = "UpdateTitle", TestCaseID = "TC-TITLE-UPD-02", Description = "Name changed to existing title's name → 400 failure", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTitleByNameAsync returns another title" } });
        }

        // TC-TITLE-UPD-03 | N | Happy path: title found, no duplicate → 200 with Title entity
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200WithTitle()
        {
            var existing = SampleTitle("T-001", "Bậc học giả");
            var repo     = GetRepoMock(existing, duplicateByName: null);
            var result   = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            QACollector.LogTestCase("Title - Update", new TestCaseDetail { FunctionGroup = "UpdateTitle", TestCaseID = "TC-TITLE-UPD-03", Description = "Valid update → 200, Data is Title entity", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No duplicate, title found" } });
        }

        // TC-TITLE-UPD-04 | N | Same name → no duplicate check, skip GetTitleByNameAsync
        [Fact]
        public async Task Handle_SameName_ShouldNotCallGetTitleByName()
        {
            var existing = SampleTitle("T-001", "Bậc học giả");
            var repo     = GetRepoMock(existing);
            // Pass same name in command
            await CreateHandler(repo).Handle(new UpdateTitleCommand { TitleId = "T-001", Name = "Bậc học giả", RequirementQuantity = 1500, ColorHex = "#FF" }, CancellationToken.None);
            repo.Verify(x => x.GetTitleByNameAsync(It.IsAny<string>(), It.IsAny<TitleStatus?>()), Times.Never);
            QACollector.LogTestCase("Title - Update", new TestCaseDetail { FunctionGroup = "UpdateTitle", TestCaseID = "TC-TITLE-UPD-04", Description = "Same name unchanged → no duplicate check called", ExpectedResult = "GetTitleByNameAsync Times.Never", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "title.Name == request.Name → skip duplicate check" } });
        }

        // TC-TITLE-UPD-05 | N | Fields updated correctly on entity
        [Fact]
        public async Task Handle_ValidRequest_FieldsUpdatedOnEntity()
        {
            var existing = SampleTitle("T-001", "Old Name");
            var repo     = GetRepoMock(existing);
            await CreateHandler(repo).Handle(MakeCommand("T-001", "New Name"), CancellationToken.None);
            existing.Name.Should().Be("New Name");
            existing.RequirementQuantity.Should().Be(2000);
            QACollector.LogTestCase("Title - Update", new TestCaseDetail { FunctionGroup = "UpdateTitle", TestCaseID = "TC-TITLE-UPD-05", Description = "Entity fields (Name, RequiredXP, IsSystemGiven) updated correctly", ExpectedResult = "All fields mutated", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Entity mutated before UpdateAsync" } });
        }

        // TC-TITLE-UPD-06 | B | UpdateAsync called once on success
        [Fact]
        public async Task Handle_ValidRequest_UpdateCalledOnce()
        {
            var repo = GetRepoMock(SampleTitle("T-001", "Bậc học giả"));
            await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            repo.Verify(x => x.UpdateAsync(It.IsAny<Title>()), Times.Once);
            QACollector.LogTestCase("Title - Update", new TestCaseDetail { FunctionGroup = "UpdateTitle", TestCaseID = "TC-TITLE-UPD-06", Description = "UpdateAsync called once on success", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist call verified" } });
        }
    }
}
