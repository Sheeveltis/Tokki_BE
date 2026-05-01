using ExamEntity = Tokki.Domain.Entities.Exam;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Commands.RegenerateExamPart;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class RegenerateExamPartCommandHandlerTests
    {
        private static RegenerateExamPartCommandHandler CreateHandler(
            Mock<IExamRepository>? examRepo           = null,
            Mock<ITemplatePartRepository>? partRepo   = null,
            Mock<IExamQuestionRepository>? qRepo      = null,
            Mock<IQuestionBankRepository>? bankRepo   = null)
        {
            return new RegenerateExamPartCommandHandler(
                (examRepo ?? new Mock<IExamRepository>()).Object,
                (partRepo ?? new Mock<ITemplatePartRepository>()).Object,
                (qRepo   ?? new Mock<IExamQuestionRepository>()).Object,
                (bankRepo?? new Mock<IQuestionBankRepository>()).Object);
        }

        private static TemplatePart GetSamplePart(string examTemplateId = "TMPL-001") => new()
        {
            TemplatePartId  = "PART-001",
            ExamTemplateId  = examTemplateId,
            QuestionTypeId  = "QT-01",
            QuestionFrom    = 1,
            QuestionTo      = 3,
            Mark            = 6
        };

        private static ExamEntity GetSample(string templateId = "TMPL-001") => new()
        {
            ExamId          = "EX-001",
            ExamTemplateId  = templateId
        };

        // Regenerate_Exam_Part_01 | A | TemplatePart not found → 400
        [Fact]
        public async Task Handle_TemplatePartNotFound_ShouldReturnFailure()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            mockPart.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((TemplatePart?)null);
            var command = new RegenerateExamPartCommand { ExamId = "EX-001", TemplatePartId = "GHOST" };

            var result = await CreateHandler(partRepo: mockPart).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "Regenerate Exam Part", TestCaseID = "Regenerate_Exam_Part_01",
                Description = "TemplatePart ID is not found in the database",
                ExpectedResult = "Return Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync for TemplatePart returns null" }
            });
        }

        // Regenerate_Exam_Part_02 | A | Exam not found → Failure
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturnFailure()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockExam = new Mock<IExamRepository>();
            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSamplePart());
            mockExam.Setup(x => x.GetByIdAsync("GHOST", It.IsAny<CancellationToken>())).ReturnsAsync((ExamEntity?)null);

            var command = new RegenerateExamPartCommand { ExamId = "GHOST", TemplatePartId = "PART-001" };
            var result = await CreateHandler(mockExam, mockPart).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "Regenerate Exam Part", TestCaseID = "Regenerate_Exam_Part_02",
                Description = "Exam ID does not exist in the database",
                ExpectedResult = "Return Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync for Exam returns null" }
            });
        }

        // Regenerate_Exam_Part_03 | A | ExamTemplateId mismatch → Failure
        [Fact]
        public async Task Handle_TemplateMismatch_ShouldReturnFailure()
        {
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockExam = new Mock<IExamRepository>();
            // Part belongs to TMPL-001, Exam belongs to TMPL-DIFFERENT
            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSamplePart("TMPL-001"));
            mockExam.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSample("TMPL-DIFFERENT"));

            var command = new RegenerateExamPartCommand { ExamId = "EX-001", TemplatePartId = "PART-001" };
            var result = await CreateHandler(mockExam, mockPart).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "Regenerate Exam Part", TestCaseID = "Regenerate_Exam_Part_03",
                Description = "Part ExamTemplateId does not match the Exam ExamTemplateId",
                ExpectedResult = "Return Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExamTemplateId mismatch condition" }
            });
        }

        // Regenerate_Exam_Part_04 | A | Not enough questions in bank → Failure
        [Fact]
        public async Task Handle_NotEnoughQuestions_ShouldReturnFailure()
        {
            var part = GetSamplePart();
            var exam = GetSample();
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockExam = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();

            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(part);
            mockExam.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ExamQuestion>());
            // Needs 3 questions (QuestionFrom=1, QuestionTo=3), returns only 1
            mockBank.Setup(x => x.GetRandomQuestionsByTypeAsync("QT-01", 3, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<QuestionBank> { new() { QuestionBankId = "QB-001" } });

            var command = new RegenerateExamPartCommand { ExamId = "EX-001", TemplatePartId = "PART-001" };
            var result = await CreateHandler(mockExam, mockPart, mockQ, mockBank).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "Regenerate Exam Part", TestCaseID = "Regenerate_Exam_Part_04",
                Description = "Question bank has fewer questions than required to fill the part",
                ExpectedResult = "Return Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "newQuestions.Count < quantityNeeded" }
            });
        }

        // Regenerate_Exam_Part_05 | N | Sufficient questions → regeneration succeeds
        [Fact]
        public async Task Handle_SufficientQuestions_ShouldReturnSuccess()
        {
            var part = GetSamplePart();
            var exam = GetSample();
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockExam = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();

            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(part);
            mockExam.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ExamQuestion>());
            mockQ.Setup(x => x.DeleteRangeAsync(It.IsAny<IEnumerable<ExamQuestion>>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ExamQuestion>>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockBank.Setup(x => x.GetRandomQuestionsByTypeAsync("QT-01", 3, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<QuestionBank>
                    {
                        new() { QuestionBankId = "QB-001" },
                        new() { QuestionBankId = "QB-002" },
                        new() { QuestionBankId = "QB-003" }
                    });

            var command = new RegenerateExamPartCommand { ExamId = "EX-001", TemplatePartId = "PART-001" };
            var result = await CreateHandler(mockExam, mockPart, mockQ, mockBank).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "Regenerate Exam Part", TestCaseID = "Regenerate_Exam_Part_05",
                Description = "Bank has enough questions; old ones removed, new ones added",
                ExpectedResult = "Return Success(true)", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "3 questions found, DeleteRange + AddRange called" }
            });
        }

        // Regenerate_Exam_Part_06 | A | Exception from SaveChanges → caught → Failure(500)
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldReturnFailure()
        {
            var part = GetSamplePart();
            var exam = GetSample();
            var mockPart = new Mock<ITemplatePartRepository>();
            var mockExam = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            var mockBank = new Mock<IQuestionBankRepository>();

            mockPart.Setup(x => x.GetByIdAsync("PART-001", It.IsAny<CancellationToken>())).ReturnsAsync(part);
            mockExam.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ExamQuestion>());
            mockQ.Setup(x => x.DeleteRangeAsync(It.IsAny<IEnumerable<ExamQuestion>>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ExamQuestion>>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Crash"));
            mockBank.Setup(x => x.GetRandomQuestionsByTypeAsync("QT-01", 3, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<QuestionBank>
                    {
                        new() { QuestionBankId = "QB-001" },
                        new() { QuestionBankId = "QB-002" },
                        new() { QuestionBankId = "QB-003" }
                    });

            var command = new RegenerateExamPartCommand { ExamId = "EX-001", TemplatePartId = "PART-001" };
            var result = await CreateHandler(mockExam, mockPart, mockQ, mockBank).Handle(command, CancellationToken.None);

            // Handler catches exception and returns Failure
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam - Regenerate Part", new TestCaseDetail
            {
                FunctionGroup = "Regenerate Exam Part", TestCaseID = "Regenerate_Exam_Part_06",
                Description = "SaveChanges throws exception; handler catches and returns failure",
                ExpectedResult = "Return Failure with error message", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exception caught in handler try/catch" }
            });
        }
    }
}
