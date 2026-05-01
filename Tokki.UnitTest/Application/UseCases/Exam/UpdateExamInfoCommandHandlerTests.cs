using ExamEntity = Tokki.Domain.Entities.Exam;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Commands.UpdateExamInfo;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class UpdateExamInfoCommandHandlerTests
    {
        private static UpdateExamInfoCommandHandler CreateHandler(
            Mock<IExamRepository>? examRepo = null,
            Mock<ITemplatePartRepository>? partRepo = null)
        {
            var mockExam = examRepo ?? new Mock<IExamRepository>();
            var mockPart = partRepo ?? new Mock<ITemplatePartRepository>();
            var logger   = new Mock<ILogger<UpdateExamInfoCommandHandler>>();
            return new UpdateExamInfoCommandHandler(mockExam.Object, mockPart.Object, logger.Object);
        }

        private static ExamEntity GetSample(string id = "EX-001") => new()
        {
            ExamId          = id,
            Title           = "Old Title",
            ExamTemplateId  = "TMPL-001"
        };

        private static List<TemplatePart> GetParts() => new()
        {
            new() { TemplatePartId = "P1", ExamTemplateId = "TMPL-001", Skill = Domain.Enums.QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 10 },
            new() { TemplatePartId = "P2", ExamTemplateId = "TMPL-001", Skill = Domain.Enums.QuestionSkill.Reading,   QuestionFrom = 11, QuestionTo = 30 }
        };

        // Update_Exam_Info_01 | A | Exam not found ? 404
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturn404()
        {
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExamEntity?)null);
            var command = new UpdateExamInfoCommand { ExamId = "GHOST", Title = "X", SkillDurations = new() };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.ExamNotFound);

            QACollector.LogTestCase("Exam - Update Info", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Info", TestCaseID = "Update_Exam_Info_01",
                Description = "ExamId does not exist in database",
                ExpectedResult = "Return 404", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // Update_Exam_Info_02 | A | Duplicate title (different exam) ? 400
        [Fact]
        public async Task Handle_DuplicateTitle_ShouldReturn400()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.IsTitleExistsAsync("Taken Title", "EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new UpdateExamInfoCommand { ExamId = "EX-001", Title = "Taken Title", SkillDurations = new() };

            var result = await CreateHandler(mockRepo, mockPart).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Update Info", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Info", TestCaseID = "Update_Exam_Info_02",
                Description = "New title is already in use by another exam",
                ExpectedResult = "Return 400", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsTitleExistsAsync returns true" }
            });
        }

        // Update_Exam_Info_03 | A | SkillDurations missing required skill ? 400
        [Fact]
        public async Task Handle_MissingSkillDuration_ShouldReturn400()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockPart.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetParts());

            // Missing"Reading" skill duration
            var command = new UpdateExamInfoCommand
            {
                ExamId = "EX-001",
                Title = "New Title",
                SkillDurations = new Dictionary<string, int> { { "Listening", 30 } }
            };

            var result = await CreateHandler(mockRepo, mockPart).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Update Info", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Info", TestCaseID = "Update_Exam_Info_03",
                Description = "SkillDurations dict is missing an entry for a template's skill (Reading)",
                ExpectedResult = "Return 400 bad request, missing skill duration",
                StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Required skill not in SkillDurations" }
            });
        }

        // Update_Exam_Info_04 | A | Skill duration value 0 or negative ? 400
        [Fact]
        public async Task Handle_ZeroSkillDuration_ShouldReturn400()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockPart.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetParts());

            var command = new UpdateExamInfoCommand
            {
                ExamId = "EX-001",
                Title = "New Title",
                SkillDurations = new Dictionary<string, int> { { "Listening", 0 }, { "Reading", 40 } }
            };

            var result = await CreateHandler(mockRepo, mockPart).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Update Info", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Info", TestCaseID = "Update_Exam_Info_04",
                Description = "A skill duration is zero which is an invalid value",
                ExpectedResult = "Return 400", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "time <= 0 fails validation" }
            });
        }

        // Update_Exam_Info_05 | N | Valid update with all valid skills ? 200
        [Fact]
        public async Task Handle_ValidUpdate_ShouldReturn200AndMutate()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            var mockPart = new Mock<ITemplatePartRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockPart.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetParts());

            var command = new UpdateExamInfoCommand
            {
                ExamId = "EX-001",
                Title = "New Title",
                SkillDurations = new Dictionary<string, int> { { "Listening", 30 }, { "Reading", 40 } }
            };

            var result = await CreateHandler(mockRepo, mockPart).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            exam.Title.Should().Be("New Title");
            exam.Duration.Should().Be(70); // 30 + 40

            QACollector.LogTestCase("Exam - Update Info", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Info", TestCaseID = "Update_Exam_Info_05",
                Description = "Valid update with correct title and proper skill durations",
                ExpectedResult = "Return 200, exam.Title and Duration mutated", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All validations pass", "Duration is sum of skills" }
            });
        }

        // Update_Exam_Info_06 | A | General exception ? 500 is returned (handler wraps with try/catch)
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturnServerError()
        {
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Unhandled"));

            var command = new UpdateExamInfoCommand { ExamId = "EX-001", Title = "X", SkillDurations = new() };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Exam - Update Info", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Info", TestCaseID = "Update_Exam_Info_06",
                Description = "Repository throws uncaught exception, handler has try/catch returning ServerError",
                ExpectedResult = "Return 500 ServerError", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exception caught in try/catch ? 500" }
            });
        }
    }
}
