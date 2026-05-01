using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.Commands.CreateExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.UnitTest.Mocks.Repositories; 
using Tokki.UnitTest.Mocks.Services;     
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class CreateExamCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidData_ShouldCreateExamSuccessfully()
        {
            // Arrange
            var command = new CreateExamCommand 
            { 
                ExamTemplateId = "TPL-01", 
                Title = "Final Exam", 
                SkillDurations = new Dictionary<string, int> { { "Listening", 30 } },
                CreatedBy = "Admin" 
            };

            var template = new ExamTemplate { ExamTemplateId = "TPL-01", Status = ExamTemplateStatus.Published, Type = (ExamType)1 };
            var parts = new List<TemplatePart> { new TemplatePart { QuestionFrom = 1, QuestionTo = 5, QuestionTypeId = "QT-01", Mark = 2 } };

            // Factory Mocks
            var mockExamRepo = MockExamRepository.GetMock(isTitleExists: false);
            var mockTemplateRepo = MockExamTemplateRepository.GetMock(template);
            var mockPartRepo = MockTemplatePartRepository.GetMock(parts);
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            // Setup QuestionBank cho đủ 5 câu
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            mockBankRepo.Setup(x => x.GetRandomQuestionsByTypeAsync(
                It.IsAny<string>(),
                5,
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { new(), new(), new(), new(), new() });
            
            // Override IdGenerator xíu để test trả về đúng ID
            mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("NEW-EXAM-1");

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("NEW-EXAM-1");

            QACollector.LogTestCase("Exam - Create Exam", new TestCaseDetail
            {
                FunctionGroup = "Create Exam",
                TestCaseID = "Create_Exam_01",
                Description = "Successfully create exam from a valid published template with enough questions",
                ExpectedResult = "Return 201 Success with new Exam ID",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unique Title", "Published Template", "Has Template Parts", "Enough Questions in Bank", "Return 201 Success" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateTitle_ShouldReturn400()
        {
            var command = new CreateExamCommand { Title = "Duplicate Exam" };

            // Trả về true = trùng tên
            var mockExamRepo = MockExamRepository.GetMock(isTitleExists: true);
            var mockTemplateRepo = MockExamTemplateRepository.GetMock();
            var mockPartRepo = MockTemplatePartRepository.GetMock();
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Create Exam", new TestCaseDetail
            {
                FunctionGroup = "Create Exam",
                TestCaseID = "Create_Exam_02",
                Description = "Fail to create exam due to duplicated title",
                ExpectedResult = "Return 400 Bad Request",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicate Title", "Return 400 Bad Request" }
            });
        }

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturn404()
        {
            var command = new CreateExamCommand { ExamTemplateId = "INVALID" };

            var mockExamRepo = MockExamRepository.GetMock(isTitleExists: false);
            var mockTemplateRepo = MockExamTemplateRepository.GetMock(null); // Không tìm thấy
            var mockPartRepo = MockTemplatePartRepository.GetMock();
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be(AppErrors.ExamTemplateNotFound.Description);

            QACollector.LogTestCase("Exam - Create Exam", new TestCaseDetail
            {
                FunctionGroup = "Create Exam",
                TestCaseID = "Create_Exam_03",
                Description = "Fail to create exam because template does not exist",
                ExpectedResult = "Return 404 Not Found",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unique Title", "Template Not Found", "Return 404 Not Found" }
            });
        }

        [Fact]
        public async Task Handle_TemplateInactive_ShouldReturn400()
        {
            var command = new CreateExamCommand { ExamTemplateId = "TPL-01" };
            var template = new ExamTemplate { Status = ExamTemplateStatus.Draft }; // Chưa Publish

            var mockExamRepo = MockExamRepository.GetMock(isTitleExists: false);
            var mockTemplateRepo = MockExamTemplateRepository.GetMock(template);
            var mockPartRepo = MockTemplatePartRepository.GetMock();
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be(AppErrors.ExamTemplateInactive.Description);

            QACollector.LogTestCase("Exam - Create Exam", new TestCaseDetail
            {
                FunctionGroup = "Create Exam",
                TestCaseID = "Create_Exam_04",
                Description = "Fail to create exam because template is not published",
                ExpectedResult = "Return 400 Bad Request",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unique Title", "Draft Template", "Return 400 Bad Request" }
            });
        }

        [Fact]
        public async Task Handle_TemplateNoParts_ShouldReturn400()
        {
            var command = new CreateExamCommand { ExamTemplateId = "TPL-01" };
            var template = new ExamTemplate { ExamTemplateId = "TPL-01", Status = ExamTemplateStatus.Published };

            var mockExamRepo = MockExamRepository.GetMock(isTitleExists: false);
            var mockTemplateRepo = MockExamTemplateRepository.GetMock(template);
            var mockPartRepo = MockTemplatePartRepository.GetMock(new List<TemplatePart>()); // Part rỗng
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be(AppErrors.ExamTemplateEmptyParts.Description);

            QACollector.LogTestCase("Exam - Create Exam", new TestCaseDetail
            {
                FunctionGroup = "Create Exam",
                TestCaseID = "Create_Exam_05",
                Description = "Fail to create exam because template has no parts defined",
                ExpectedResult = "Return 400 Bad Request",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unique Title", "Published Template", "No Template Parts", "Return 400 Bad Request" }
            });
        }

        [Fact]
        public async Task Handle_NotEnoughQuestionsInBank_ShouldReturn400()
        {
            var command = new CreateExamCommand { ExamTemplateId = "TPL-01" };
            var template = new ExamTemplate { ExamTemplateId = "TPL-01", Status = ExamTemplateStatus.Published };
            var parts = new List<TemplatePart> { new TemplatePart { QuestionFrom = 1, QuestionTo = 10, QuestionTypeId = "QT-01" } }; // Cần 10 câu

            var mockExamRepo = MockExamRepository.GetMock(isTitleExists: false);
            var mockTemplateRepo = MockExamTemplateRepository.GetMock(template);
            var mockPartRepo = MockTemplatePartRepository.GetMock(parts);

            // Giả lập kho chỉ có 1 câu
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            mockBankRepo.Setup(x => x.GetRandomQuestionsByTypeAsync(
                It.IsAny<string>(),
                10,
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QuestionBank> { new() });
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("insufficient");

            QACollector.LogTestCase("Exam - Create Exam", new TestCaseDetail
            {
                FunctionGroup = "Create Exam",
                TestCaseID = "Create_Exam_06",
                Description = "Fail to create exam because question bank does not have enough questions",
                ExpectedResult = "Return 400 Bad Request with missing count info",
                StatusRound1 = "Passed",
                TestCaseType = "B", // Boundary case (Thiếu tài nguyên)
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unique Title", "Published Template", "Has Template Parts", "Not Enough Questions", "Return 400 Bad Request" }
            });
        }

        [Fact]
        public async Task Handle_Exception_ShouldReturn500()
        {
            var command = new CreateExamCommand { Title = "Exception Exam" };

            // Ghi đè để ném Exception
            var mockExamRepo = MockExamRepository.GetMock();
            mockExamRepo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB Connection Lost"));

            var mockTemplateRepo = MockExamTemplateRepository.GetMock();
            var mockPartRepo = MockTemplatePartRepository.GetMock();
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Exam - Create Exam", new TestCaseDetail
            {
                FunctionGroup = "Create Exam",
                TestCaseID = "Create_Exam_07",
                Description = "Handle unexpected system exception gracefully",
                ExpectedResult = "Return 500 Server Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "System Exception", "Return 500 Server Error" }
            });
        }
        [Fact]
        public async Task Handle_IncompleteSkillDurations_ShouldReturn400()
        {
            var command = new CreateExamCommand 
            { 
                ExamTemplateId = "TPL-01", 
                Title = "Incomplete Skill Durations", 
                SkillDurations = new Dictionary<string, int> { { "Listening", 30 } } // Chỉ nhập 1 skill trong khi template có 2
            };

            var template = new ExamTemplate { ExamTemplateId = "TPL-01", Status = ExamTemplateStatus.Published };
            // Giả sử template có 2 skill: Listening và Reading
            var parts = new List<TemplatePart> 
            { 
                new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 5 },
                new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 6, QuestionTo = 10 }
            };

            var mockExamRepo = MockExamRepository.GetMock(isTitleExists: false);
            var mockTemplateRepo = MockExamTemplateRepository.GetMock(template);
            var mockPartRepo = MockTemplatePartRepository.GetMock(parts);
            var mockBankRepo = MockQuestionBankRepository.GetMock();
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockLogger = new Mock<ILogger<CreateExamCommandHandler>>();

            var handler = new CreateExamCommandHandler(
                mockExamRepo.Object, mockTemplateRepo.Object, mockPartRepo.Object,
                mockBankRepo.Object, mockIdGen.Object, mockLogger.Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Please enter a valid test time");
        }
    }
}