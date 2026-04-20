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
    public class ExportExamToPdfCommandHandlerTests : IDisposable
    {
        private readonly Mock<IExamRepository> _mockExamRepo;
        private readonly Mock<IPdfService> _mockPdfService;
        private readonly ExportExamToPdfCommandHandler _handler;
        private readonly string _testTemplatePath;

        public ExportExamToPdfCommandHandlerTests()
        {
            _mockExamRepo = new Mock<IExamRepository>();
            _mockPdfService = new Mock<IPdfService>();
            _handler = new ExportExamToPdfCommandHandler(_mockExamRepo.Object, _mockPdfService.Object);

            // Setup a dummy template file to bypass File.ReadAllTextAsync
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Templates");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _testTemplatePath = Path.Combine(dir, "ExamExportTemplate.html");
            File.WriteAllText(_testTemplatePath, "<html><body>{{ Title }} - {{ TopikLevelName }} - {{ SkillKo }} - {{ QuestionsHtml }}</body></html>");
        }

        public void Dispose()
        {
            if (File.Exists(_testTemplatePath))
            {
                File.Delete(_testTemplatePath);
            }
        }

        [Fact]
        public async Task Handle_ExamNotFound_ReturnsFailure404()
        {
            var command = new ExportExamToPdfCommand("E1", false);
            _mockExamRepo.Setup(x => x.GetExamWithFullDetailsAsync("E1", It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Domain.Entities.Exam?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Contain("Không tìm thấy đề thi.");

            QACollector.LogTestCase("Exam - Export PDF", new TestCaseDetail
            {
                FunctionGroup     = "ExportExamToPdfCommandHandler",
                TestCaseID        = "ExportExamToPdfCommandHandler_01",
                Description       = "Missing ExamId returns 404 cleanly",
                ExpectedResult    = "Returns 404 failure gracefully",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exam is null" }
            });
        }

        [Fact]
        public async Task Handle_TemplateNotFound_ReturnsFailure500()
        {
            // Temporarily delete the file to force 500
            Dispose();

            var command = new ExportExamToPdfCommand("E1", false);
            var exam = new Domain.Entities.Exam { ExamId = "E1" };
            _mockExamRepo.Setup(x => x.GetExamWithFullDetailsAsync("E1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Message.Should().Contain("Không tìm thấy tệp mẫu PDF.");

            // Restore the file for other tests if they run in parallel or loop
            File.WriteAllText(_testTemplatePath, "<html><body>Template</body></html>");

            QACollector.LogTestCase("Exam - Export PDF", new TestCaseDetail
            {
                FunctionGroup     = "ExportExamToPdfCommandHandler",
                TestCaseID        = "ExportExamToPdfCommandHandler_02",
                Description       = "HTML Template file is missing",
                ExpectedResult    = "Returns 500 error cleanly",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Template path missing" }
            });
        }

        [Fact]
        public async Task Handle_ValidExam_GeneratesPdfSuccessfully()
        {
            var command = new ExportExamToPdfCommand("E1", false);
            
            var templatePart = new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 1, QuestionTo = 10, PartTitle = "Part 1" };
            var examTemplate = new ExamTemplate 
            { 
                TemplateParts = new List<TemplatePart> { templatePart } 
            };
            
            var questionBank = new QuestionBank { PassageId = "P1" };
            var examScore = new ExamQuestion { QuestionNo = 1, QuestionBank = questionBank };

            var exam = new Domain.Entities.Exam 
            { 
                ExamId = "E1", 
                Title = "Topik Ex",
                Type = ExamType.TopikI,
                PdfDownloadCount = 0,
                ExamTemplate = examTemplate,
                ExamQuestions = new List<ExamQuestion> { examScore }
            };

            _mockExamRepo.Setup(x => x.GetExamWithFullDetailsAsync("E1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _mockPdfService.Setup(x => x.GeneratePdfFromHtml(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(new byte[] { 1, 2, 3 });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.FileName.Should().Be("[Tokki]_Topik Ex.pdf");
            result.Data.PdfData.Length.Should().Be(3);

            _mockExamRepo.Verify(x => x.UpdateAsync(It.Is<Domain.Entities.Exam>(e => e.PdfDownloadCount == 1)), Times.Once);

            QACollector.LogTestCase("Exam - Export PDF", new TestCaseDetail
            {
                FunctionGroup     = "ExportExamToPdfCommandHandler",
                TestCaseID        = "ExportExamToPdfCommandHandler_03",
                Description       = "Valid inputs generates PDF smoothly",
                ExpectedResult    = "Returns PDF byte array accurately",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Successfully generated PDF bytes correctly" }
            });
        }
        [Fact]
        public async Task Handle_ValidExam_MultipleBranchesCoverage_GeneratesPdfSuccessfully()
        {
            var command = new ExportExamToPdfCommand("E1", true);
            
            var templatePart1 = new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 1, PartTitle = "Part 1" };
            var templatePart2 = new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 2, QuestionTo = 2, PartTitle = "Part 2" };
            var templatePart3 = new TemplatePart { Skill = QuestionSkill.Writing, QuestionFrom = 3, QuestionTo = 3, PartTitle = "Part 3" };
            var templatePart4 = new TemplatePart { Skill = (QuestionSkill)999, QuestionFrom = 4, QuestionTo = 4, PartTitle = "Part 4" };

            var examTemplate = new ExamTemplate 
            { 
                TemplateParts = new List<TemplatePart> { templatePart1, templatePart2, templatePart3, templatePart4 } 
            };
            
            var examQ1 = new ExamQuestion { QuestionNo = 1, QuestionBank = new QuestionBank { Passage = null, MediaUrl = "http://a.png", Content = "Q1", Explanation = "Exp1", QuestionOptions = new List<QuestionOption> { new QuestionOption { KeyOption = "2", ImageUrl = "http://opt.png", Content = null } } } };
            var examQ2 = new ExamQuestion { QuestionNo = 2, QuestionBank = new QuestionBank { PassageId = "P1", Passage = new Passage { Content = "Passage1" }, Content = "Q2", QuestionOptions = new List<QuestionOption> { new QuestionOption { KeyOption = "3", Content = "Opt3" } } } };
            var examQ3 = new ExamQuestion { QuestionNo = 3, QuestionBank = new QuestionBank { QuestionOptions = new List<QuestionOption> { new QuestionOption { KeyOption = "4" } } } };
            var examQ4 = new ExamQuestion { QuestionNo = 4, QuestionBank = new QuestionBank { QuestionOptions = new List<QuestionOption> { new QuestionOption { KeyOption = "5" } } } };

            var exam = new Domain.Entities.Exam 
            { 
                ExamId = "E1", 
                Title = "Topik Ex 2",
                Type = ExamType.EntranceTestTopikII,
                PdfDownloadCount = 0,
                ExamTemplate = examTemplate,
                ExamQuestions = new List<ExamQuestion> { examQ1, examQ2, examQ3, examQ4 }
            };

            _mockExamRepo.Setup(x => x.GetExamWithFullDetailsAsync("E1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            _mockPdfService.Setup(x => x.GeneratePdfFromHtml(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new byte[] { 1, 2, 3 });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            QACollector.LogTestCase("Exam - Export PDF", new TestCaseDetail
            {
                FunctionGroup     = "ExportExamToPdfCommandHandler",
                TestCaseID        = "ExportExamToPdfCommandHandler_04",
                Description       = "Multiple branches hit (Passage=null, different Skills, Topic levels, keyOptions)",
                ExpectedResult    = "Returns PDF smoothly capturing all UI branches",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Branch coverage maximized" }
            });
        }
    }
}
