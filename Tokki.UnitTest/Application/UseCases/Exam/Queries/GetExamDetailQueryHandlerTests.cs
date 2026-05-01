using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Queries.GetExamDetailQuery;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam.Queries
{
    public class GetExamDetailQueryHandlerTests
    {
        private readonly Mock<IExamRepository> _examMock = new();
        private readonly Mock<ITemplatePartRepository> _partMock = new();
        private readonly Mock<IExamTemplateRepository> _templateMock = new();

        private GetExamDetailQueryHandler CreateHandler()
        {
            return new GetExamDetailQueryHandler(_examMock.Object, _partMock.Object, _templateMock.Object);
        }

        // -----------------------------------------------------------
        // GetExamDetailQueryHandler_01 | A | Exam Not Found
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturn404()
        {
            _examMock.Setup(x => x.GetExamWithFullDetailsAsync("ex", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Domain.Entities.Exam?)null);
            
            var handler = CreateHandler();
            var cmd = new GetExamDetailQuery { ExamId = "ex" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailQueryHandler",
                TestCaseID = "GetExamDetailQueryHandler_01",
                Description = "Missing item securely traps to 404 block",
                ExpectedResult = "Return 404 Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns Null initially" }
            });
        }

        // -----------------------------------------------------------
        // GetExamDetailQueryHandler_02 | N | Parts properly aggregated and nested
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_QuestionsMapping_ShouldAccuratelyMatchRanges()
        {
            var exam = new Domain.Entities.Exam { ExamId = "ex", ExamTemplateId = "t1" };
            exam.ExamQuestions = new List<ExamQuestion> 
            { 
                new ExamQuestion { QuestionNo = 1, QuestionBank = new QuestionBank { Content = "Q1", QuestionType = new QuestionType { Skill = QuestionSkill.Listening } } },
                new ExamQuestion { QuestionNo = 2, QuestionBank = new QuestionBank { Content = "Q2", QuestionType = new QuestionType { Skill = QuestionSkill.Writing } } } // Writing maps to Image
            };
            
            var tmpl = new ExamTemplate { Name = "Tmp1" };
            var parts = new List<TemplatePart> 
            {
                new TemplatePart { QuestionFrom = 1, QuestionTo = 2, Mark = 5, Skill = QuestionSkill.Listening }
            };

            _examMock.Setup(x => x.GetExamWithFullDetailsAsync("ex", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _templateMock.Setup(x => x.GetByIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(tmpl);
            _partMock.Setup(x => x.GetByExamTemplateIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);

            var handler = CreateHandler();
            var cmd = new GetExamDetailQuery { ExamId = "ex" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.TemplateParts.Should().HaveCount(1);
            result.Data.TemplateParts[0].Questions.Should().HaveCount(2); 
            result.Data.TemplateParts[0].Questions[0].MediaType.Should().Be("Audio"); // Listening
            result.Data.TemplateParts[0].Questions[1].MediaType.Should().Be("Image"); // Null goes to Image fallback

            result.Data.SkillTotalScores["Listening"].Should().Be(10); // 2 Qs * 5 Mark
            result.Data.MaxScore.Should().Be(10); // Sum total Marks (1 loop for part * 2 Qs = 10 logic inside eq loop adds Mark directly)

            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailQueryHandler",
                TestCaseID = "GetExamDetailQueryHandler_02",
                Description = "Nested extraction logic evaluates successfully bounding properties exactly to matching questions",
                ExpectedResult = "DTO Maps 1 Part with 2 matched questions appropriately",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid template mapping" }
            });
        }

        // -----------------------------------------------------------
        // GetExamDetailQueryHandler_03 | N | Option sorting behavior guarantees strictly sorted results
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_OptionsStrictlySorted_ShouldValidateBehaviors()
        {
            var exam = new Domain.Entities.Exam { ExamId = "ex", ExamTemplateId = "t1" };
            var qb = new QuestionBank { Content = "Q", QuestionType = new QuestionType { Skill = QuestionSkill.Reading } };
            qb.QuestionOptions.Add(new QuestionOption { KeyOption = "2", Content = "Two" });
            qb.QuestionOptions.Add(new QuestionOption { KeyOption = "1", Content = "One" });
            exam.ExamQuestions = new List<ExamQuestion> { new ExamQuestion { QuestionNo = 1, QuestionBank = qb } };
            
            var tmpl = new ExamTemplate { Name = "Tmp1" };
            var parts = new List<TemplatePart> { new TemplatePart { QuestionFrom = 1, QuestionTo = 1, Mark = 5, Skill = QuestionSkill.Reading } };

            _examMock.Setup(x => x.GetExamWithFullDetailsAsync("ex", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _templateMock.Setup(x => x.GetByIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(tmpl);
            _partMock.Setup(x => x.GetByExamTemplateIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetExamDetailQuery { ExamId = "ex" }, CancellationToken.None);

            result.Data!.TemplateParts[0].Questions[0].Options[0].KeyOption.Should().Be("1");

            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailQueryHandler",
                TestCaseID = "GetExamDetailQueryHandler_03",
                Description = "Ensures unordered SQL collections explicitly sort Option items ascending safely for API JSON result",
                ExpectedResult = "Item [0].KeyOption=1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Lists sorted effectively" }
            });
        }

        // -----------------------------------------------------------
        // GetExamDetailQueryHandler_04 | N | Question Skills switch branches covering all values cleanly
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SkillWriting_ShouldMapImages()
        {
            var exam = new Domain.Entities.Exam { ExamId = "ex", ExamTemplateId = "t1" };
            exam.ExamQuestions = new List<ExamQuestion> { new ExamQuestion { QuestionNo = 1, QuestionBank = new QuestionBank { Content = "Q1", QuestionType = new QuestionType { Skill = QuestionSkill.Writing } } } };            
            var tmpl = new ExamTemplate { Name = "Tmp1" };
            var parts = new List<TemplatePart> { new TemplatePart { QuestionFrom = 1, QuestionTo = 1, Mark = 5, Skill = QuestionSkill.Writing } };

            _examMock.Setup(x => x.GetExamWithFullDetailsAsync("ex", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _templateMock.Setup(x => x.GetByIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(tmpl);
            _partMock.Setup(x => x.GetByExamTemplateIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetExamDetailQuery { ExamId = "ex" }, CancellationToken.None);

            result.Data!.TemplateParts[0].Questions[0].MediaType.Should().Be("Image");

            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailQueryHandler",
                TestCaseID = "GetExamDetailQueryHandler_04",
                Description = "Fallback Writing explicitly resolves into Image Media Type correctly",
                ExpectedResult = "Return Image MediaType directly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill is Writing" }
            });
        }

        // -----------------------------------------------------------
        // GetExamDetailQueryHandler_05 | B | Missing ExamQuestions in Exam Returns Safe Zeroes
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NullExamQuestions_ShouldInitializeZeroesSafely()
        {
            var exam = new Domain.Entities.Exam { ExamId = "ex", ExamTemplateId = "t1", ExamQuestions = null }; 
            var tmpl = new ExamTemplate { Name = "Tmp1" };
            var parts = new List<TemplatePart>(); 

            _examMock.Setup(x => x.GetExamWithFullDetailsAsync("ex", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _templateMock.Setup(x => x.GetByIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(tmpl);
            _partMock.Setup(x => x.GetByExamTemplateIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetExamDetailQuery { ExamId = "ex" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalQuestions.Should().Be(0);

            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailQueryHandler",
                TestCaseID = "GetExamDetailQueryHandler_05",
                Description = "Initial null collection references safely handled preventing NullRef in counting",
                ExpectedResult = "Total = 0 returned nicely",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExamQuestions null" }
            });
        }
        
        // -----------------------------------------------------------
        // GetExamDetailQueryHandler_06 | N | QuestionBank Is Null Internal Handling
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_QuestionBankNull_ShouldContinueWithoutError()
        {
            var exam = new Domain.Entities.Exam { ExamId = "ex", ExamTemplateId = "t1", ExamQuestions = new List<ExamQuestion> { new ExamQuestion { QuestionNo = 1, QuestionBank = null } } }; 
            var tmpl = new ExamTemplate { Name = "Tmp1" };
            var parts = new List<TemplatePart> { new TemplatePart { QuestionFrom = 1, QuestionTo = 1, Mark = 5, Skill = QuestionSkill.Writing } };

            _examMock.Setup(x => x.GetExamWithFullDetailsAsync("ex", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _templateMock.Setup(x => x.GetByIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(tmpl);
            _partMock.Setup(x => x.GetByExamTemplateIdAsync("t1", It.IsAny<CancellationToken>())).ReturnsAsync(parts);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetExamDetailQuery { ExamId = "ex" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.TemplateParts[0].Questions.Should().HaveCount(0); // Ignored due to"if (qBank == null) continue;" logic safely

            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailQueryHandler",
                TestCaseID = "GetExamDetailQueryHandler_06",
                Description = "Internal boundary checks if linked structure object fails to load",
                ExpectedResult = "Skip without breaking execution",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "qBank = null" }
            });
        }
    }
}
