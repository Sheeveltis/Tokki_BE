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
using Tokki.Application.UseCases.Exam.Commands.RemoveQuestionFromExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class RemoveQuestionFromExamCommandHandlerTests
    {
        private static RemoveQuestionFromExamCommandHandler CreateHandler(
            Mock<IExamRepository>? examRepo = null,
            Mock<IExamQuestionRepository>? qRepo = null)
        {
            var mockExam = examRepo ?? new Mock<IExamRepository>();
            var mockQ    = qRepo   ?? new Mock<IExamQuestionRepository>();
            var logger   = new Mock<ILogger<RemoveQuestionFromExamCommandHandler>>();
            return new RemoveQuestionFromExamCommandHandler(mockExam.Object, mockQ.Object, logger.Object);
        }

        private static ExamEntity GetSample(ExamStatus status = ExamStatus.Draft) => new()
        {
            ExamId = "EX-001", Title = "Test", Status = status
        };

        private static ExamQuestion GetSampleQuestion() => new()
        {
            ExamQuestionId = "EQ-001",
            ExamId         = "EX-001",
            QuestionNo     = 5
        };

        // TC-EXRQ-01 | A | Exam not found → 404
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturn404()
        {
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExamEntity?)null);
            var command = new RemoveQuestionFromExamCommand { ExamId = "GHOST", QuestionNo = 1 };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.ExamNotFound);

            QACollector.LogTestCase("Exam - Remove Question", new TestCaseDetail
            {
                FunctionGroup = "Remove Question From Exam", TestCaseID = "TC-EXRQ-01",
                Description = "Exam ID not found in the database",
                ExpectedResult = "Return 404 with ExamNotFound error", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // TC-EXRQ-02 | A | QuestionNo not in exam → 404
        [Fact]
        public async Task Handle_QuestionNoNotInExam_ShouldReturn404()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamAndQuestionNoAsync("EX-001", 99, It.IsAny<CancellationToken>())).ReturnsAsync((ExamQuestion?)null);

            var command = new RemoveQuestionFromExamCommand { ExamId = "EX-001", QuestionNo = 99 };

            var result = await CreateHandler(mockRepo, mockQ).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Remove Question", new TestCaseDetail
            {
                FunctionGroup = "Remove Question From Exam", TestCaseID = "TC-EXRQ-02",
                Description = "The requested QuestionNo does not exist in the exam",
                ExpectedResult = "Return 404 Not Found", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByExamAndQuestionNoAsync returns null" }
            });
        }

        // TC-EXRQ-03 | N | Valid removal → 200
        [Fact]
        public async Task Handle_ValidRemoval_ShouldReturn200()
        {
            var exam     = GetSample();
            var question = GetSampleQuestion();
            var mockRepo = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamAndQuestionNoAsync("EX-001", 5, It.IsAny<CancellationToken>())).ReturnsAsync(question);
            mockQ.Setup(x => x.DeleteAsync(It.IsAny<ExamQuestion>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new RemoveQuestionFromExamCommand { ExamId = "EX-001", QuestionNo = 5 };

            var result = await CreateHandler(mockRepo, mockQ).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Exam - Remove Question", new TestCaseDetail
            {
                FunctionGroup = "Remove Question From Exam", TestCaseID = "TC-EXRQ-03",
                Description = "Valid removal of a question from a Draft exam",
                ExpectedResult = "Return 200 Success", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Question found and deleted", "Return 200" }
            });
        }

        // TC-EXRQ-04 | N | If exam is Published → status revert to Draft
        [Fact]
        public async Task Handle_PublishedExam_ShouldRevertToDraft()
        {
            var exam     = GetSample(status: ExamStatus.Published);
            var question = GetSampleQuestion();
            var mockRepo = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamAndQuestionNoAsync("EX-001", 5, It.IsAny<CancellationToken>())).ReturnsAsync(question);
            mockQ.Setup(x => x.DeleteAsync(It.IsAny<ExamQuestion>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new RemoveQuestionFromExamCommand { ExamId = "EX-001", QuestionNo = 5 };

            var result = await CreateHandler(mockRepo, mockQ).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            exam.Status.Should().Be(ExamStatus.Draft); // Auto-reverted

            QACollector.LogTestCase("Exam - Remove Question", new TestCaseDetail
            {
                FunctionGroup = "Remove Question From Exam", TestCaseID = "TC-EXRQ-04",
                Description = "Removing a question from Published exam auto-reverts exam status to Draft",
                ExpectedResult = "Return 200, exam.Status = Draft", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Published exam reverts to Draft on removal" }
            });
        }

        // TC-EXRQ-05 | N | DeleteAsync called once
        [Fact]
        public async Task Handle_ValidRemoval_DeleteAsyncCalledOnce()
        {
            var exam     = GetSample();
            var question = GetSampleQuestion();
            var mockRepo = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamAndQuestionNoAsync("EX-001", 5, It.IsAny<CancellationToken>())).ReturnsAsync(question);
            mockQ.Setup(x => x.DeleteAsync(It.IsAny<ExamQuestion>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new RemoveQuestionFromExamCommand { ExamId = "EX-001", QuestionNo = 5 };
            await CreateHandler(mockRepo, mockQ).Handle(command, CancellationToken.None);

            mockQ.Verify(x => x.DeleteAsync(question), Times.Once);

            QACollector.LogTestCase("Exam - Remove Question", new TestCaseDetail
            {
                FunctionGroup = "Remove Question From Exam", TestCaseID = "TC-EXRQ-05",
                Description = "Verify DeleteAsync is invoked exactly once during successful removal",
                ExpectedResult = "Times.Once verified on DeleteAsync", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mock.Verify Times.Once" }
            });
        }

        // TC-EXRQ-06 | A | SaveChanges throws → 500 (handler catches exception)
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldReturn500()
        {
            var exam     = GetSample();
            var question = GetSampleQuestion();
            var mockRepo = new Mock<IExamRepository>();
            var mockQ    = new Mock<IExamQuestionRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockQ.Setup(x => x.GetByExamAndQuestionNoAsync("EX-001", 5, It.IsAny<CancellationToken>())).ReturnsAsync(question);
            mockQ.Setup(x => x.DeleteAsync(It.IsAny<ExamQuestion>())).Returns(Task.CompletedTask);
            mockQ.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Down"));

            var command = new RemoveQuestionFromExamCommand { ExamId = "EX-001", QuestionNo = 5 };
            var result  = await CreateHandler(mockRepo, mockQ).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Exam - Remove Question", new TestCaseDetail
            {
                FunctionGroup = "Remove Question From Exam", TestCaseID = "TC-EXRQ-06",
                Description = "SaveChangesAsync throws exception, handler wraps to 500",
                ExpectedResult = "Return 500 ServerError", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exception caught → 500" }
            });
        }
    }
}
