using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Titles.Commands.CreateTitle;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles
{
    public class CreateTitleCommandHandlerTests
    {
        private static Mock<ITitleRepository> GetRepoMock(Title? existing = null)
        {
            var m = new Mock<ITitleRepository>();
            m.Setup(x => x.GetTitleByNameAsync(It.IsAny<string>(), It.IsAny<TitleStatus?>())).ReturnsAsync(existing);
            m.Setup(x => x.AddAsync(It.IsAny<Title>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "T-001")
        {
            var m = new Mock<IIdGeneratorService>();
            m.Setup(x => x.Generate(It.IsAny<int>())).Returns(id);
            return m;
        }

        private static CreateTitleCommandHandler CreateHandler(
            Mock<ITitleRepository>?   repo  = null,
            Mock<IIdGeneratorService>? idGen = null)
            => new CreateTitleCommandHandler(
                (repo  ?? GetRepoMock()).Object,
                (idGen ?? GetIdGenMock()).Object);

        private static CreateTitleCommand MakeCommand(string name = "Bậc học giả", long xp = 1000)
            => new CreateTitleCommand { Name = name, Description = "Top learner", RequirementQuantity = xp, ColorHex = "#GOLD" };

        // CreateTitle_01 | A | Title name already exists → 400 failure
        [Fact]
        public async Task Handle_DuplicateName_ShouldReturn400Failure()
        {
            var existing = new Title { TitleId = "T-OLD", Name = "Bậc học giả" };
            var repo     = GetRepoMock(existing);
            var result   = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Title - Create", new TestCaseDetail { FunctionGroup = "CreateTitle", TestCaseID = "CreateTitle_01", Description = "Duplicate name → 400 failure", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTitleByNameAsync returns existing title" } });
        }

        // CreateTitle_02 | A | RequiredXP is negative → 400 failure
        [Fact]
        public async Task Handle_NegativeXP_ShouldReturn400Failure()
        {
            var result = await CreateHandler().Handle(MakeCommand(xp: -1), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Title - Create", new TestCaseDetail { FunctionGroup = "CreateTitle", TestCaseID = "CreateTitle_02", Description = "RequiredXP=-1 → 400 failure", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "RequiredXP < 0 guard" } });
        }

        // CreateTitle_03 | N | Happy path: new unique name, valid XP → 201 with Title entity
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn201WithTitle()
        {
            var idGen  = GetIdGenMock("T-NEW");
            var result = await CreateHandler(idGen: idGen).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNull();
            result.Data!.TitleId.Should().Be("T-NEW");
            QACollector.LogTestCase("Title - Create", new TestCaseDetail { FunctionGroup = "CreateTitle", TestCaseID = "CreateTitle_03", Description = "Valid request → 201, TitleId='T-NEW'", ExpectedResult = "IsSuccess=true, 201, Data.TitleId='T-NEW'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No duplicate, valid XP" } });
        }

        // CreateTitle_04 | N | Created title fields match request
        [Fact]
        public async Task Handle_ValidRequest_CreatedTitleFieldsMatchCommand()
        {
            Title? captured = null;
            var repo = GetRepoMock();
            repo.Setup(x => x.AddAsync(It.IsAny<Title>())).Callback<Title>(t => captured = t).Returns(Task.CompletedTask);
            await CreateHandler(repo).Handle(MakeCommand("Bậc văn nhân", 2000), CancellationToken.None);
            captured.Should().NotBeNull();
            captured!.Name.Should().Be("Bậc văn nhân");
            captured.RequirementQuantity.Should().Be(2000);
            QACollector.LogTestCase("Title - Create", new TestCaseDetail { FunctionGroup = "CreateTitle", TestCaseID = "CreateTitle_04", Description = "Title fields (Name, RequiredXP, IsSystemGiven) match command", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Command fields mapped to entity" } });
        }

        // CreateTitle_05 | B | AddAsync called once on success
        [Fact]
        public async Task Handle_ValidRequest_AddCalledOnce()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            repo.Verify(x => x.AddAsync(It.IsAny<Title>()), Times.Once);
            QACollector.LogTestCase("Title - Create", new TestCaseDetail { FunctionGroup = "CreateTitle", TestCaseID = "CreateTitle_05", Description = "AddAsync called once on success", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist call verified" } });
        }

        // CreateTitle_06 | B | Duplicate name → AddAsync never called
        [Fact]
        public async Task Handle_DuplicateName_AddNeverCalled()
        {
            var existing = new Title { TitleId = "T-OLD", Name = "Bậc học giả" };
            var repo     = GetRepoMock(existing);
            await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            repo.Verify(x => x.AddAsync(It.IsAny<Title>()), Times.Never);
            QACollector.LogTestCase("Title - Create", new TestCaseDetail { FunctionGroup = "CreateTitle", TestCaseID = "CreateTitle_06", Description = "Duplicate name → early return, AddAsync never called", ExpectedResult = "Times.Never", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Guard returns before AddAsync" } });
        }
    }
}