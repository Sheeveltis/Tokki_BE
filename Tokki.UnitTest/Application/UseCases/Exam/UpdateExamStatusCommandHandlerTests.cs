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
using Tokki.Application.UseCases.Exam.Commands.UpdateExamStatus;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class UpdateExamStatusCommandHandlerTests
    {
        private static UpdateExamStatusCommandHandler CreateHandler(Mock<IExamRepository>? repo = null)
            => new((repo ?? new Mock<IExamRepository>()).Object);

        private static ExamEntity GetSample(string id = "EX-001", ExamStatus status = ExamStatus.Draft) => new()
        {
            ExamId  = id,
            Title   = "Sample Exam",
            Status  = status
        };

        // Update_Exam_Status_01 | A | Exam not found → 404
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturn404()
        {
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExamEntity?)null);
            var command = new UpdateExamStatusCommand { ExamId = "GHOST", Status = ExamStatus.Published };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.ExamNotFound);

            QACollector.LogTestCase("Exam - Update Status", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Status", TestCaseID = "Update_Exam_Status_01",
                Description = "Exam ID does not exist in database",
                ExpectedResult = "Return 404 Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // Update_Exam_Status_02 | A | Invalid status value → 400
        [Fact]
        public async Task Handle_InvalidStatus_ShouldReturn400()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);

            // Cast invalid integer to ExamStatus to bypass enum validation
            var command = new UpdateExamStatusCommand { ExamId = "EX-001", Status = (ExamStatus)999 };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Exam - Update Status", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Status", TestCaseID = "Update_Exam_Status_02",
                Description = "Pass an invalid ExamStatus value (999) not in enum",
                ExpectedResult = "Return 400 Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!Enum.IsDefined fails", "Return 400" }
            });
        }

        // Update_Exam_Status_03 | N | Valid status update Draft → Published → 200
        [Fact]
        public async Task Handle_ValidStatusUpdate_ShouldReturn200()
        {
            var exam = GetSample(status: ExamStatus.Draft);
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new UpdateExamStatusCommand { ExamId = "EX-001", Status = ExamStatus.Published };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            exam.Status.Should().Be(ExamStatus.Published);

            QACollector.LogTestCase("Exam - Update Status", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Status", TestCaseID = "Update_Exam_Status_03",
                Description = "Valid transition from Draft to Published",
                ExpectedResult = "Return 200, exam.Status mutated", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status changes to Published", "Return 200" }
            });
        }

        // Update_Exam_Status_04 | N | Published → Deleted transition
        [Fact]
        public async Task Handle_PublishedToDeleted_ShouldReturn200()
        {
            var exam = GetSample(status: ExamStatus.Published);
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new UpdateExamStatusCommand { ExamId = "EX-001", Status = ExamStatus.Deleted };

            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            exam.Status.Should().Be(ExamStatus.Deleted);

            QACollector.LogTestCase("Exam - Update Status", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Status", TestCaseID = "Update_Exam_Status_04",
                Description = "Transition Published exam to Deleted state",
                ExpectedResult = "Return 200, exam.Status = Deleted", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid transition to Deleted" }
            });
        }

        // Update_Exam_Status_05 | N | UpdateAsync called exactly once
        [Fact]
        public async Task Handle_ValidUpdate_UpdateAsyncCalledOnce()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new UpdateExamStatusCommand { ExamId = "EX-001", Status = ExamStatus.Published };

            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.UpdateAsync(exam), Times.Once);

            QACollector.LogTestCase("Exam - Update Status", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Status", TestCaseID = "Update_Exam_Status_05",
                Description = "Verify UpdateAsync invoked exactly once during valid status change",
                ExpectedResult = "Times.Once verified", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Moq.Verify Times.Once" }
            });
        }

        // Update_Exam_Status_06 | A | SaveChanges throws → exception propagates
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
            var exam = GetSample();
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Down"));

            var command = new UpdateExamStatusCommand { ExamId = "EX-001", Status = ExamStatus.Published };

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("Exam - Update Status", new TestCaseDetail
            {
                FunctionGroup = "Update Exam Status", TestCaseID = "Update_Exam_Status_06",
                Description = "SaveChangesAsync throws exception during status update",
                ExpectedResult = "Exception propagates without suppression", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws", "No try/catch" }
            });
        }
    }
}
