using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TemplateParts.Commands.CreateTemplatePart;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TemplateParts
{
    public class CreateTemplatePartCommandHandlerTests
    {
        private static Mock<ITemplatePartRepository> GetTemplatePartRepoMock(bool overlap = false)
        {
            var m = new Mock<ITemplatePartRepository>();
            m.Setup(x => x.IsQuestionRangeOverlapAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
             .ReturnsAsync(overlap);
            m.Setup(x => x.AddAsync(It.IsAny<TemplatePart>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            return m;
        }

        private static Mock<IExamTemplateRepository> GetExamTemplateRepoMock(ExamTemplate? template = null)
        {
            var m = new Mock<IExamTemplateRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(template ?? new ExamTemplate { ExamTemplateId = "T1" });
            return m;
        }

        private static Mock<IQuestionTypeRepository> GetQTypeRepoMock(QuestionType? qt = null)
        {
            var m = new Mock<IQuestionTypeRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(qt);
            return m;
        }

        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "TP-001")
        {
            var m = new Mock<IIdGeneratorService>();
            m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return m;
        }

        private static CreateTemplatePartCommandHandler CreateHandler(
            Mock<ITemplatePartRepository>?  tpRepo      = null,
            Mock<IExamTemplateRepository>?  etRepo      = null,
            Mock<IQuestionTypeRepository>?  qtRepo      = null,
            Mock<IIdGeneratorService>?      idGen       = null)
            => new CreateTemplatePartCommandHandler(
                (tpRepo ?? GetTemplatePartRepoMock()).Object,
                (etRepo ?? GetExamTemplateRepoMock()).Object,
                (qtRepo ?? GetQTypeRepoMock()).Object,
                (idGen  ?? GetIdGenMock()).Object,
                NullLogger<CreateTemplatePartCommandHandler>.Instance);

        private static CreateTemplatePartCommand MakeCommand(
            string examTemplateId = "T1",
            int    from           = 1,
            int    to             = 10,
            string questionTypeId = "") => new CreateTemplatePartCommand
        {
            ExamTemplateId = examTemplateId,
            PartTitle      = "Part 1 - Listening",
            Skill          = QuestionSkill.Listening,
            QuestionFrom   = from,
            QuestionTo     = to,
            Instruction    = "Listen and answer",
            Mark           = 2,
            QuestionTypeId = questionTypeId
        };

        // TC-TP-CREATE-01 | A | ExamTemplate not found → failure
        [Fact]
        public async Task Handle_ExamTemplateNotFound_ShouldReturnFailure()
        {
            var etRepo = GetExamTemplateRepoMock(null);
            var result = await CreateHandler(etRepo: etRepo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-01", Description = "ExamTemplate not found → failure (ExamTemplateNotFound)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TP-CREATE-02 | A | QuestionFrom > QuestionTo → invalid range failure
        [Fact]
        public async Task Handle_InvalidRange_QuestionFromGreaterThanTo_ShouldReturnFailure()
        {
            var result = await CreateHandler().Handle(MakeCommand(from: 10, to: 5), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-02", Description = "QuestionFrom(10) > QuestionTo(5) → TemplatePartInvalidRange", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionFrom > QuestionTo guard" } });
        }

        // TC-TP-CREATE-03 | A | QuestionFrom <= 0 → invalid range failure
        [Fact]
        public async Task Handle_QuestionFromZeroOrNegative_ShouldReturnFailure()
        {
            var result = await CreateHandler().Handle(MakeCommand(from: 0, to: 10), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-03", Description = "QuestionFrom=0 → TemplatePartInvalidRange", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "QuestionFrom<=0 guard" } });
        }

        // TC-TP-CREATE-04 | A | Question range overlaps existing part → failure
        [Fact]
        public async Task Handle_RangeOverlap_ShouldReturnFailure()
        {
            var tpRepo = GetTemplatePartRepoMock(overlap: true);
            var result = await CreateHandler(tpRepo: tpRepo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-04", Description = "Range overlaps existing part → TemplatePartRangeOverlap", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsQuestionRangeOverlapAsync returns true" } });
        }

        // TC-TP-CREATE-05 | N | Happy path: valid part → 201 with generated ID
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn201WithGeneratedId()
        {
            var idGen  = GetIdGenMock("TP-NEWID");
            var result = await CreateHandler(idGen: idGen).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("TP-NEWID");
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-05", Description = "Valid part → 201, Data='TP-NEWID'", ExpectedResult = "IsSuccess=true, 201, Data='TP-NEWID'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All guards pass, part created" } });
        }

        // TC-TP-CREATE-06 | B | AddAsync and SaveChangesAsync both called once on success
        [Fact]
        public async Task Handle_ValidRequest_AddAndSaveBothCalledOnce()
        {
            var tpRepo = GetTemplatePartRepoMock();
            await CreateHandler(tpRepo: tpRepo).Handle(MakeCommand(), CancellationToken.None);
            tpRepo.Verify(x => x.AddAsync(It.IsAny<TemplatePart>()), Times.Once);
            tpRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-06", Description = "AddAsync and SaveChangesAsync both called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }
        // TC-TP-CREATE-07 | A | Skill mismatch mapped QuestionType → failure
        [Fact]
        public async Task Handle_MismatchedSkillQuestionType_ShouldReturnFailure()
        {
            var qtRepo = GetQTypeRepoMock(new QuestionType { Skill = QuestionSkill.Reading }); // Expected is Listening
            var result = await CreateHandler(qtRepo: qtRepo).Handle(MakeCommand(questionTypeId: "QT-1"), CancellationToken.None);
            
            result.IsSuccess.Should().BeFalse();
            result.Errors[0].Description.Should().Contain("Kỹ năng không khớp");
            
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-07", Description = "Skill mismatch for QuestionType", ExpectedResult = "IsSuccess=false, Contains skill mismatch message", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "questionType.Skill != request.Skill" } });
        }

        // TC-TP-CREATE-08 | A | Repository throws exception → 500
        [Fact]
        public async Task Handle_RepositoryThrowsException_ShouldReturn500()
        {
            var tpRepo = new Mock<ITemplatePartRepository>();
            tpRepo.Setup(x => x.IsQuestionRangeOverlapAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>())).ReturnsAsync(false);
            tpRepo.Setup(x => x.AddAsync(It.IsAny<TemplatePart>())).ThrowsAsync(new Exception("DB failed"));
            
            var result = await CreateHandler(tpRepo: tpRepo).Handle(MakeCommand(), CancellationToken.None);
            
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            
            QACollector.LogTestCase("TemplatePart - Create", new TestCaseDetail { FunctionGroup = "CreateTemplatePart", TestCaseID = "TC-TP-CREATE-08", Description = "Catch unhandled exception returning 500 ServerError", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "catch (Exception)" } });
        }
    }
}
