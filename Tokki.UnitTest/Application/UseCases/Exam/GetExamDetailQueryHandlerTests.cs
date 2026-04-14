using ExamEntity = Tokki.Domain.Entities.Exam;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Queries.GetExamDetailQuery;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class GetExamDetailQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static GetExamDetailQueryHandler CreateHandler(
            Mock<IExamRepository>? examRepo        = null,
            Mock<ITemplatePartRepository>? partRepo = null,
            Mock<IExamTemplateRepository>? tmplRepo = null)
        {
            return new GetExamDetailQueryHandler(
                (examRepo ?? new Mock<IExamRepository>()).Object,
                (partRepo ?? new Mock<ITemplatePartRepository>()).Object,
                (tmplRepo ?? new Mock<IExamTemplateRepository>()).Object);
        }

        private static ExamEntity GetSampleExam() => new()
        {
            ExamId         = "EX-001",
            Title          = "Test Exam Detail",
            ExamTemplateId = "TMPL-001",
            Duration       = 70,
            Status         = ExamStatus.Published,
            SkillDurations = "{\"Listening\":30,\"Reading\":40}",
            ExamQuestions  = new List<ExamQuestion>
            {
                new() {
                    ExamQuestionId = "EQ-001",
                    QuestionNo     = 1,
                    QuestionBank   = new() {
                        Content         = "Q1",
                        QuestionOptions = new List<QuestionOption>(),
                        QuestionType    = new() { Skill = QuestionSkill.Listening }
                    }
                }
            }
        };

        private static ExamTemplate GetSampleTemplate() =>
            new() { ExamTemplateId = "TMPL-001", Name = "TOPIK I" };

        private static List<TemplatePart> GetSortedParts() => new()
        {
            new() {
                TemplatePartId = "P1",
                Skill          = QuestionSkill.Listening,
                QuestionFrom   = 1,
                QuestionTo     = 10,
                Instruction    = "Listen and pick",
                Mark           = 2,
                ExampleUrl     = null
            }
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXDET-01 | A | Exam not found → 404
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturn404()
        {
            // Arrange
            var mockExam = new Mock<IExamRepository>();
            mockExam.Setup(x => x.GetExamWithFullDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExamEntity?)null);

            // Act
            var result = await CreateHandler(examRepo: mockExam)
                             .Handle(new GetExamDetailQuery { ExamId = "GHOST" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Exam Detail",
                TestCaseID        = "TC-EXDET-01",
                Description       = "ExamId does not exist in the database",
                ExpectedResult    = "Return 404 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetExamWithFullDetailsAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXDET-02 | N | Valid exam → 200 with ExamId and Title
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExam_ShouldReturn200WithData()
        {
            // Arrange
            var mockExam = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockTmpl = new Mock<IExamTemplateRepository>();

            mockExam.Setup(x => x.GetExamWithFullDetailsAsync("EX-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleExam());
            mockTmpl.Setup(x => x.GetByIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleTemplate());
            mockPart.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSortedParts());

            // Act
            var result = await CreateHandler(mockExam, mockPart, mockTmpl)
                             .Handle(new GetExamDetailQuery { ExamId = "EX-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.ExamId.Should().Be("EX-001");
            result.Data.Title.Should().Be("Test Exam Detail");

            // Excel Log
            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Exam Detail",
                TestCaseID        = "TC-EXDET-02",
                Description       = "Valid ExamId returns full exam detail DTO",
                ExpectedResult    = "Return 200 with correct ExamId and Title",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All repos return valid data" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXDET-03 | N | ExamTemplateName populated from template repo
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExam_ShouldIncludeTemplateName()
        {
            // Arrange
            var mockExam = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockTmpl = new Mock<IExamTemplateRepository>();

            mockExam.Setup(x => x.GetExamWithFullDetailsAsync("EX-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleExam());
            mockTmpl.Setup(x => x.GetByIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleTemplate());
            mockPart.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSortedParts());

            // Act
            var result = await CreateHandler(mockExam, mockPart, mockTmpl)
                             .Handle(new GetExamDetailQuery { ExamId = "EX-001" }, CancellationToken.None);

            // Assert
            result.Data!.ExamTemplateName.Should().Be("TOPIK I");

            // Excel Log
            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Exam Detail",
                TestCaseID        = "TC-EXDET-03",
                Description       = "ExamTemplateName is fetched from template repository and included in DTO",
                ExpectedResult    = "ExamTemplateName = 'TOPIK I'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.GetByIdAsync then template.Name" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXDET-04 | N | Parts sorted ascending by QuestionFrom
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExam_PartsSortedByQuestionFrom()
        {
            // Arrange
            var mockExam = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockTmpl = new Mock<IExamTemplateRepository>();

            mockExam.Setup(x => x.GetExamWithFullDetailsAsync("EX-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleExam());
            mockTmpl.Setup(x => x.GetByIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleTemplate());
            // Return unsorted parts
            mockPart.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<TemplatePart>
                    {
                        new() { TemplatePartId = "P2", Skill = QuestionSkill.Reading,   QuestionFrom = 11, QuestionTo = 30, Instruction = "Read" },
                        new() { TemplatePartId = "P1", Skill = QuestionSkill.Listening, QuestionFrom = 1,  QuestionTo = 10, Instruction = "Listen" }
                    });

            // Act
            var result = await CreateHandler(mockExam, mockPart, mockTmpl)
                             .Handle(new GetExamDetailQuery { ExamId = "EX-001" }, CancellationToken.None);

            // Assert
            result.Data!.TemplateParts[0].TemplatePartsTitle.Should().Contain("[1~10]");
            result.Data.TemplateParts[1].TemplatePartsTitle.Should().Contain("[11~30]");

            // Excel Log
            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Exam Detail",
                TestCaseID        = "TC-EXDET-04",
                Description       = "Parts returned unsorted are sorted by QuestionFrom ascending",
                ExpectedResult    = "TemplateParts[0] = [1~10], TemplateParts[1] = [11~30]",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "parts.OrderBy(p => p.QuestionFrom)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXDET-05 | N | TotalQuestions equals ExamQuestions count
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExam_TotalQuestionsEqualExamQuestionsCount()
        {
            // Arrange
            var mockExam = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockTmpl = new Mock<IExamTemplateRepository>();

            mockExam.Setup(x => x.GetExamWithFullDetailsAsync("EX-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleExam());
            mockTmpl.Setup(x => x.GetByIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSampleTemplate());
            mockPart.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(GetSortedParts());

            // Act
            var result = await CreateHandler(mockExam, mockPart, mockTmpl)
                             .Handle(new GetExamDetailQuery { ExamId = "EX-001" }, CancellationToken.None);

            // Assert
            result.Data!.TotalQuestions.Should().Be(1); // GetSampleExam has 1 ExamQuestion

            // Excel Log
            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Exam Detail",
                TestCaseID        = "TC-EXDET-05",
                Description       = "TotalQuestions in DTO equals count of ExamQuestions in entity",
                ExpectedResult    = "TotalQuestions = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "exam.ExamQuestions?.Count ?? 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXDET-06 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var mockExam = new Mock<IExamRepository>();
            mockExam.Setup(x => x.GetExamWithFullDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB Error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockExam).Handle(new GetExamDetailQuery { ExamId = "EX-001" }, CancellationToken.None));

            // Excel Log
            QACollector.LogTestCase("Exam - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Exam Detail",
                TestCaseID        = "TC-EXDET-06",
                Description       = "Repository throws exception; handler has no try/catch → exception propagates",
                ExpectedResult    = "Exception propagates to caller",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ThrowsAsync('DB Error')" }
            });
        }
    }
}
