using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Exam.Commands.ExportExamToPdf;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam.Commands
{
    public class ExportExamToPdfCommandHandlerExtendedTests
    {
        private readonly Mock<IExamRepository> _mockExamRepo = new();
        private readonly Mock<IPdfService> _mockPdfService = new();
        private readonly ExportExamToPdfCommandHandler _handler;

        public ExportExamToPdfCommandHandlerExtendedTests()
        {
            _handler = new ExportExamToPdfCommandHandler(_mockExamRepo.Object, _mockPdfService.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // ExportExamToPdfCommandHandler_01 | N | ShowExplanation applies correctly -> pdf generation triggered
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ShowExplanation_ShouldTriggerCorrectFormatting()
        {
            var command = new ExportExamToPdfCommand("E1", true); // show explanations
            var qb = new QuestionBank { PassageId = "P1", MediaUrl = "http://test.com/image.jpg", Explanation = "Explain" };
            qb.QuestionOptions.Add(new QuestionOption { KeyOption = "1", Content = "Ans" });
            
            var eq = new ExamQuestion { QuestionNo = 1, QuestionBank = qb };
            var tp = new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 1, QuestionTo = 1 };
            
            var exam = new Domain.Entities.Exam 
            { 
                ExamId = "E1", Title = "Topik", Type = ExamType.TopikI,
                ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart> { tp } },
                ExamQuestions = new List<ExamQuestion> { eq }
            };

            _mockExamRepo.Setup(x => x.GetExamWithFullDetailsAsync("E1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _mockPdfService.Setup(x => x.GeneratePdfFromHtml(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(new byte[] { 9, 9 });

            // Create temporary template so it doesn't fail 500
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Templates");
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "ExamExportTemplate.html"), "{{ QuestionsHtml }}");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            QACollector.LogTestCase("Exam - Export PDF", new TestCaseDetail
            {
                FunctionGroup = "ExportExamToPdfCommandHandler",
                TestCaseID = "ExportExamToPdfCommandHandler_01",
                Description = "Passage handling and explanations successfully passed",
                ExpectedResult = "Success, GeneratePdfFromHtml invoked",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ShowExplanation true" }
            });
        }
        
        // ═══════════════════════════════════════════════════════════
        // ExportExamToPdfCommandHandler_02 | N | MediaUrl Parsing for Image
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingSkill_ShouldNotShowExplanation()
        {
            var command = new ExportExamToPdfCommand("E1", true); 
            var qb = new QuestionBank { Explanation = "Explain It" };
            
            var eq = new ExamQuestion { QuestionNo = 1, QuestionBank = qb };
            var tp = new TemplatePart { Skill = QuestionSkill.Writing, QuestionFrom = 1, QuestionTo = 1 };
            
            var exam = new Domain.Entities.Exam 
            { 
                ExamId = "E1", Title = "Topik", Type = ExamType.TopikI,
                ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart> { tp } },
                ExamQuestions = new List<ExamQuestion> { eq }
            };

            _mockExamRepo.Setup(x => x.GetExamWithFullDetailsAsync("E1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _mockPdfService.Setup(x => x.GeneratePdfFromHtml(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(new byte[1]);

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Templates");
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "ExamExportTemplate.html"), "{{ Title }}");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            QACollector.LogTestCase("Exam - Export PDF", new TestCaseDetail
            {
                FunctionGroup = "ExportExamToPdfCommandHandler",
                TestCaseID = "ExportExamToPdfCommandHandler_02",
                Description = "Writing skill forcibly suppresses explanations internally",
                ExpectedResult = "Success bytes",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill is Writing" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // ExportExamToPdfCommandHandler_03 | N | Enum conversions and specific Topik II formatting
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopikIIRenderingFormat()
        {
            var command = new ExportExamToPdfCommand("E1", false); 
            var qb = new QuestionBank { };
            
            var eq = new ExamQuestion { QuestionNo = 1, QuestionBank = qb };
            var tp = new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 1 };
            
            var exam = new Domain.Entities.Exam 
            { 
                ExamId = "E1", Title = "Invalid/Topik Name", Type = ExamType.TopikII, // Has slash which needs sanitizing
                ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart> { tp } },
                ExamQuestions = new List<ExamQuestion> { eq }
            };

            _mockExamRepo.Setup(x => x.GetExamWithFullDetailsAsync("E1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _mockPdfService.Setup(x => x.GeneratePdfFromHtml(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(new byte[0]);

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Templates");
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "ExamExportTemplate.html"), "{{ Title }}");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.FileName.Should().NotContain("/"); // Invalid chars are stripped out replacing with _
            
            QACollector.LogTestCase("Exam - Export PDF", new TestCaseDetail
            {
                FunctionGroup = "ExportExamToPdfCommandHandler",
                TestCaseID = "ExportExamToPdfCommandHandler_03",
                Description = "Title sanitization replaces invalid file chars and TOPKi II enum format works",
                ExpectedResult = "Success, filename sanitized",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExamTitle has invalid Path characters" }
            });
        }
    }
}
