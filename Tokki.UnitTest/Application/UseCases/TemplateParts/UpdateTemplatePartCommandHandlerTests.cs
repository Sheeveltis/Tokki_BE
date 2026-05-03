using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.TemplateParts.Commands.UpdateTemplatePart;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TemplateParts
{
    public class UpdateTemplatePartCommandHandlerTests
    {
        private static Mock<ITemplatePartRepository> GetRepoMock(TemplatePart? part = null, bool overlap = false)
        {
            var m = new Mock<ITemplatePartRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(part);
            m.Setup(x => x.IsQuestionRangeOverlapAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
             .ReturnsAsync(overlap);
            m.Setup(x => x.UpdateAsync(It.IsAny<TemplatePart>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            return m;
        }

        private static UpdateTemplatePartCommandHandler CreateHandler(Mock<ITemplatePartRepository>? repo = null)
            => new UpdateTemplatePartCommandHandler(
                (repo ?? GetRepoMock()).Object,
                NullLogger<UpdateTemplatePartCommandHandler>.Instance);

        private static TemplatePart SamplePart(string id = "TP-001") => new TemplatePart
        {
            TemplatePartId = id,
            ExamTemplateId = "T1",
            Skill          = QuestionSkill.Listening,
            QuestionFrom   = 1,
            QuestionTo     = 10,
            PartTitle      = "Part 1",
            Mark           = 2
        };

        private static UpdateTemplatePartCommand MakeCommand(
            string id   = "TP-001",
            int    from = 1,
            int    to   = 10) => new UpdateTemplatePartCommand
        {
            TemplatePartId = id,
            PartTitle      = "Updated Part",
            Skill          = QuestionSkill.Reading,
            QuestionFrom   = from,
            QuestionTo     = to,
            Instruction    = "Read carefully",
            Mark           = 3,
            QuestionTypeId = "QT-01"
        };

        // UpdateTemplatePart_01 | A | Part not found → failure
        [Fact]
        public async Task Handle_PartNotFound_ShouldReturnFailure()
        {
            var repo   = GetRepoMock(null);
            var result = await CreateHandler(repo).Handle(MakeCommand("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Update", new TestCaseDetail { FunctionGroup = "UpdateTemplatePart", TestCaseID = "UpdateTemplatePart_01", Description = "Part not found → failure (TemplatePartNotFound)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // UpdateTemplatePart_02 | A | QuestionFrom > QuestionTo → invalid range failure
        [Fact]
        public async Task Handle_InvalidRange_ShouldReturnFailure()
        {
            var repo   = GetRepoMock(SamplePart());
            var result = await CreateHandler(repo).Handle(MakeCommand(from: 10, to: 5), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Update", new TestCaseDetail { FunctionGroup = "UpdateTemplatePart", TestCaseID = "UpdateTemplatePart_02", Description = "QuestionFrom(10) > QuestionTo(5) → TemplatePartInvalidRange", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionFrom > QuestionTo guard" } });
        }

        // UpdateTemplatePart_03 | A | Range overlaps another part → failure
        [Fact]
        public async Task Handle_RangeOverlap_ShouldReturnFailure()
        {
            var repo   = GetRepoMock(SamplePart(), overlap: true);
            var result = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Update", new TestCaseDetail { FunctionGroup = "UpdateTemplatePart", TestCaseID = "UpdateTemplatePart_03", Description = "Range overlaps → TemplatePartRangeOverlap", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsQuestionRangeOverlapAsync returns true" } });
        }

        // UpdateTemplatePart_04 | N | Happy path → 200 with part ID
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200WithPartId()
        {
            var repo   = GetRepoMock(SamplePart("TP-001"));
            var result = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("TP-001");
            QACollector.LogTestCase("TemplatePart - Update", new TestCaseDetail { FunctionGroup = "UpdateTemplatePart", TestCaseID = "UpdateTemplatePart_04", Description = "Valid update → 200, Data='TP-001'", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All guards pass, part updated" } });
        }

        // UpdateTemplatePart_05 | N | Part fields updated correctly
        [Fact]
        public async Task Handle_ValidRequest_PartFieldsUpdatedCorrectly()
        {
            var part = SamplePart();
            var repo = GetRepoMock(part);
            await CreateHandler(repo).Handle(new UpdateTemplatePartCommand
            {
                TemplatePartId = "TP-001",
                PartTitle      = "New Title",
                Skill          = QuestionSkill.Writing,
                QuestionFrom   = 3,
                QuestionTo     = 7,
                Mark           = 5,
                QuestionTypeId = "QT-99"
            }, CancellationToken.None);
            part.PartTitle.Should().Be("New Title");
            part.Skill.Should().Be(QuestionSkill.Writing);
            part.QuestionFrom.Should().Be(3);
            part.QuestionTo.Should().Be(7);
            part.Mark.Should().Be(5);
            QACollector.LogTestCase("TemplatePart - Update", new TestCaseDetail { FunctionGroup = "UpdateTemplatePart", TestCaseID = "UpdateTemplatePart_05", Description = "Part fields all updated correctly on entity", ExpectedResult = "Entity fields mutated", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "PartTitle, Skill, From, To, Mark updated" } });
        }

        // UpdateTemplatePart_06 | A | Repository throws on UpdateAsync → failure returned
        [Fact]
        public async Task Handle_RepoThrowsOnUpdate_ShouldReturnServerError()
        {
            var repo = new Mock<ITemplatePartRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(SamplePart());
            repo.Setup(x => x.IsQuestionRangeOverlapAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(x => x.UpdateAsync(It.IsAny<TemplatePart>())).ThrowsAsync(new Exception("DB error"));
            var result = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Update", new TestCaseDetail { FunctionGroup = "UpdateTemplatePart", TestCaseID = "UpdateTemplatePart_06", Description = "UpdateAsync throws → failure (ServerError)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught in try-catch" } });
        }
    }
}
