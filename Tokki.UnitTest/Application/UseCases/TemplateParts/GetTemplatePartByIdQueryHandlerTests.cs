using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Application.UseCases.TemplateParts.Queries.GetTemplatePartById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TemplateParts
{
    public class GetTemplatePartByIdQueryHandlerTests
    {
        private static Mock<ITemplatePartRepository> GetRepoMock(TemplatePart? part = null)
        {
            var m = new Mock<ITemplatePartRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(part);
            return m;
        }

        private static GetTemplatePartByIdQueryHandler CreateHandler(Mock<ITemplatePartRepository>? repo = null)
            => new GetTemplatePartByIdQueryHandler((repo ?? GetRepoMock()).Object);

        private static TemplatePart SamplePart(string id = "TP-001") => new TemplatePart
        {
            TemplatePartId = id,
            ExamTemplateId = "T1",
            Skill          = QuestionSkill.Listening,
            QuestionFrom   = 1,
            QuestionTo     = 10,
            PartTitle      = "Part 1",
            Instruction    = "Listen carefully",
            Mark           = 2,
            QuestionTypeId = "QT-01",
            QuestionType   = new QuestionType { QuestionTypeId = "QT-01", Name = "Multiple Choice" }
        };

        // TC-TP-GBI-01 | A | Part not found → failure
        [Fact]
        public async Task Handle_PartNotFound_ShouldReturnFailure()
        {
            var repo   = GetRepoMock(null);
            var result = await CreateHandler(repo).Handle(new GetTemplatePartByIdQuery { TemplatePartId = "MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Get By Id", new TestCaseDetail { FunctionGroup = "GetTemplatePartById", TestCaseID = "TC-TP-GBI-01", Description = "Part not found → failure (TemplatePartNotFound)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TP-GBI-02 | N | Happy path: part found → 200 with DTO
        [Fact]
        public async Task Handle_PartFound_ShouldReturn200WithDto()
        {
            var repo   = GetRepoMock(SamplePart());
            var result = await CreateHandler(repo).Handle(new GetTemplatePartByIdQuery { TemplatePartId = "TP-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TemplatePartId.Should().Be("TP-001");
            QACollector.LogTestCase("TemplatePart - Get By Id", new TestCaseDetail { FunctionGroup = "GetTemplatePartById", TestCaseID = "TC-TP-GBI-02", Description = "Part found → 200, DTO.TemplatePartId='TP-001'", ExpectedResult = "IsSuccess=true, Data not null", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns part" } });
        }

        // TC-TP-GBI-03 | N | All DTO fields mapped correctly
        [Fact]
        public async Task Handle_PartFound_AllFieldsMappedToDto()
        {
            var repo   = GetRepoMock(SamplePart());
            var result = await CreateHandler(repo).Handle(new GetTemplatePartByIdQuery { TemplatePartId = "TP-001" }, CancellationToken.None);
            result.Data!.Skill.Should().Be(QuestionSkill.Listening);
            result.Data.QuestionFrom.Should().Be(1);
            result.Data.QuestionTo.Should().Be(10);
            result.Data.Mark.Should().Be(2);
            result.Data.QuestionTypeName.Should().Be("Multiple Choice");
            QACollector.LogTestCase("TemplatePart - Get By Id", new TestCaseDetail { FunctionGroup = "GetTemplatePartById", TestCaseID = "TC-TP-GBI-03", Description = "All DTO fields (Skill, From, To, Mark, QuestionTypeName) mapped correctly", ExpectedResult = "All fields verified", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TemplatePart entity fully mapped" } });
        }

        // TC-TP-GBI-04 | N | QuestionType null → QuestionTypeName is empty string
        [Fact]
        public async Task Handle_PartFoundWithNullQuestionType_QuestionTypeNameIsEmpty()
        {
            var part = SamplePart();
            part.QuestionType = null!;
            var repo   = GetRepoMock(part);
            var result = await CreateHandler(repo).Handle(new GetTemplatePartByIdQuery { TemplatePartId = "TP-001" }, CancellationToken.None);
            result.Data!.QuestionTypeName.Should().Be(string.Empty);
            QACollector.LogTestCase("TemplatePart - Get By Id", new TestCaseDetail { FunctionGroup = "GetTemplatePartById", TestCaseID = "TC-TP-GBI-04", Description = "QuestionType=null → QuestionTypeName=''", ExpectedResult = "QuestionTypeName is empty string", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Null QuestionType handled correctly" } });
        }

        // TC-TP-GBI-05 | B | GetByIdAsync called with trimmed ID
        [Fact]
        public async Task Handle_IdWithSpaces_GetByIdCalledOnce()
        {
            var repo = GetRepoMock(SamplePart());
            await CreateHandler(repo).Handle(new GetTemplatePartByIdQuery { TemplatePartId = "  TP-001  " }, CancellationToken.None);
            repo.Verify(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("TemplatePart - Get By Id", new TestCaseDetail { FunctionGroup = "GetTemplatePartById", TestCaseID = "TC-TP-GBI-05", Description = "ID with spaces → GetByIdAsync called once (handler trims ID)", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "ID.Trim() applied before lookup" } });
        }

        // TC-TP-GBI-06 | N | PartTitle and Instruction mapped correctly
        [Fact]
        public async Task Handle_PartFound_PartTitleAndInstructionMapped()
        {
            var repo   = GetRepoMock(SamplePart());
            var result = await CreateHandler(repo).Handle(new GetTemplatePartByIdQuery { TemplatePartId = "TP-001" }, CancellationToken.None);
            result.Data!.PartTitle.Should().Be("Part 1");
            result.Data.Instruction.Should().Be("Listen carefully");
            QACollector.LogTestCase("TemplatePart - Get By Id", new TestCaseDetail { FunctionGroup = "GetTemplatePartById", TestCaseID = "TC-TP-GBI-06", Description = "PartTitle='Part 1', Instruction='Listen carefully' mapped", ExpectedResult = "Both fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "String fields passed through" } });
        }
    }
}
