using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Commands.DeleteExam;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;
using ExamEntity = Tokki.Domain.Entities.Exam;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class DeleteExamCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static DeleteExamCommandHandler CreateHandler(Mock<IExamRepository>? repo = null)
        {
            return new DeleteExamCommandHandler((repo ?? new Mock<IExamRepository>()).Object);
        }

        private static ExamEntity GetSampleExam(string id = "EX-001", ExamStatus status = ExamStatus.Draft) => new()
        {
            ExamId = id,
            Title  = "Sample Exam",
            Status = status
        };

        // ═══════════════════════════════════════════════════════════
        // Delete_Exam_01 | A | Exam not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturn404()
        {
            // Arrange
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExamEntity?)null);

            var command = new DeleteExamCommand { ExamId = "EX-GHOST" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Delete Exam", new TestCaseDetail
            {
                FunctionGroup     = "Delete Exam",
                TestCaseID        = "Delete_Exam_01",
                Description       = "Attempt to delete an exam with an ID that does not exist",
                ExpectedResult    = "Return 404 ExamNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Exam_02 | N | Valid exam → soft-deleted 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExam_ShouldSoftDeleteAndReturn200()
        {
            // Arrange
            var exam = GetSampleExam();
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new DeleteExamCommand { ExamId = "EX-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            exam.Status.Should().Be(ExamStatus.Deleted);

            QACollector.LogTestCase("Exam - Delete Exam", new TestCaseDetail
            {
                FunctionGroup     = "Delete Exam",
                TestCaseID        = "Delete_Exam_02",
                Description       = "Valid delete request — exam should be soft-deleted",
                ExpectedResult    = "Return 200 Success, exam.Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exam exists", "Status set to Deleted", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Exam_03 | N | UpdateAsync is called exactly once
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExam_UpdateAsyncCalledOnce()
        {
            // Arrange
            var exam = GetSampleExam();
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new DeleteExamCommand { ExamId = "EX-001" };

            // Act
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<ExamEntity>()), Times.Once);

            QACollector.LogTestCase("Exam - Delete Exam", new TestCaseDetail
            {
                FunctionGroup     = "Delete Exam",
                TestCaseID        = "Delete_Exam_03",
                Description       = "Verify UpdateAsync is invoked exactly once during soft delete",
                ExpectedResult    = "UpdateAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Moq.Verify checks invocation count" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Exam_04 | B | Null ExamId → 404 from lookup
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullExamId_ShouldReturn404()
        {
            // Arrange
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExamEntity?)null);

            var command = new DeleteExamCommand { ExamId = null! };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Delete Exam", new TestCaseDetail
            {
                FunctionGroup     = "Delete Exam",
                TestCaseID        = "Delete_Exam_04",
                Description       = "Boundary: null ExamId causes lookup to fail",
                ExpectedResult    = "Return 404 ExamNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExamId = null", "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Exam_05 | N | Exam status is Deleted after handler runs
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExam_StatusMutatedToDeleted()
        {
            // Arrange
            var exam = GetSampleExam(status: ExamStatus.Published);
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new DeleteExamCommand { ExamId = "EX-001" };

            // Act
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            exam.Status.Should().Be(ExamStatus.Deleted);

            QACollector.LogTestCase("Exam - Delete Exam", new TestCaseDetail
            {
                FunctionGroup     = "Delete Exam",
                TestCaseID        = "Delete_Exam_05",
                Description       = "Delete a Published exam — status should be mutated to Deleted in memory",
                ExpectedResult    = "exam.Status = Deleted after handler",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exam was Published", "Status set to Deleted", "EF tracking check" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Exam_06 | A | SaveChanges throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
            // Arrange
            var exam = GetSampleExam();
            var mockRepo = new Mock<IExamRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamEntity>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Crash"));

            var command = new DeleteExamCommand { ExamId = "EX-001" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("Exam - Delete Exam", new TestCaseDetail
            {
                FunctionGroup     = "Delete Exam",
                TestCaseID        = "Delete_Exam_06",
                Description       = "Database crashes during SaveChangesAsync after status mutation",
                ExpectedResult    = "Exception propagates to global error handler",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws Exception", "No try/catch in handler" }
            });
        }
    }
}
