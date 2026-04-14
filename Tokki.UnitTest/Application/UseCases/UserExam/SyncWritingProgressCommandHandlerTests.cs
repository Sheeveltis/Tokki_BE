using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Commands.SyncWritingProgress;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class SyncWritingProgressCommandHandlerTests
    {
        private static SyncWritingProgressCommandHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new SyncWritingProgressCommandHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static SyncWritingProgressCommand BuildCommand(
            string userId           = "USER-001",
            string userQuestionId   = "UWA-001",
            string? answerContent   = "My answer")
        {
            return new SyncWritingProgressCommand
            {
                UserId          = userId,
                UserQuestionId  = userQuestionId,
                AnswerContent   = answerContent
            };
        }

        private static UserExamWritingAnswer BuildWritingAnswer(
            string id               = "UWA-001",
            string userId           = "USER-001",
            UserExamStatus status   = UserExamStatus.InProgress,
            string? existingContent = "")
        {
            return new UserExamWritingAnswer
            {
                UserExamWritingAnswerId = id,
                AnswerContent           = existingContent ?? string.Empty,
                WordCount               = existingContent?.Length ?? 0,
                Question                = new QuestionBank { QuestionBankId = "QB-001", Content = "Writing prompt" },
                UserExam                = new Domain.Entities.UserExam
                {
                    UserId = userId,
                    Status = status
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SYNW-01 | A | Writing answer not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingAnswerNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingAnswerWithSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserExamWritingAnswer?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync Writing", new TestCaseDetail
            {
                FunctionGroup     = "SyncWritingProgress",
                TestCaseID        = "TC-SYNW-01",
                Description       = "Writing answer ID not found → 404 Not Found",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetWritingAnswerWithSessionAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SYNW-02 | A | UserId mismatch → 403
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserIdMismatch_ShouldReturn403()
        {
            // Arrange
            var answer = BuildWritingAnswer(userId: "USER-OWNER");
            var repo   = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingAnswerWithSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(answer);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(userId: "HACKER"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync Writing", new TestCaseDetail
            {
                FunctionGroup     = "SyncWritingProgress",
                TestCaseID        = "TC-SYNW-02",
                Description       = "Request UserId differs from session owner → 403",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session.UserId != Request.UserId" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SYNW-03 | A | Exam already Completed → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamCompleted_ShouldReturn400()
        {
            // Arrange
            var answer = BuildWritingAnswer(status: UserExamStatus.Completed);
            var repo   = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingAnswerWithSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(answer);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync Writing", new TestCaseDetail
            {
                FunctionGroup     = "SyncWritingProgress",
                TestCaseID        = "TC-SYNW-03",
                Description       = "Exam status is Completed, writing sync rejected → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Completed" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SYNW-04 | N | Content changed → SaveChanges called, WordCount updated
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ContentChanged_ShouldSaveAndUpdateWordCount()
        {
            // Arrange
            var answer = BuildWritingAnswer(existingContent: "Old content");
            var repo   = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingAnswerWithSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(answer);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(answerContent: "New longer content here"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync Writing", new TestCaseDetail
            {
                FunctionGroup     = "SyncWritingProgress",
                TestCaseID        = "TC-SYNW-04",
                Description       = "Answer content changed → SaveChanges called, WordCount updated",
                ExpectedResult    = "IsSuccess=true, SaveChanges Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "New AnswerContent differs from existing", "WordCount updated" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SYNW-05 | B | Same content → SaveChanges NOT called
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SameContent_ShouldNotSaveChanges()
        {
            // Arrange — existing == incoming
            var answer = BuildWritingAnswer(existingContent: "My answer");
            var repo   = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingAnswerWithSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(answer);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(answerContent: "My answer"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync Writing", new TestCaseDetail
            {
                FunctionGroup     = "SyncWritingProgress",
                TestCaseID        = "TC-SYNW-05",
                Description       = "Answer content unchanged → SaveChanges NOT called",
                ExpectedResult    = "IsSuccess=true, SaveChanges Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AnswerContent unchanged" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SYNW-06 | E | Repository throws exception → propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingAnswerWithSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB timeout"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(BuildCommand(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB timeout");

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync Writing", new TestCaseDetail
            {
                FunctionGroup     = "SyncWritingProgress",
                TestCaseID        = "TC-SYNW-06",
                Description       = "Repository throws exception → propagates to caller",
                ExpectedResult    = "Exception thrown with 'DB timeout'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetWritingAnswerWithSessionAsync throws Exception" }
            });
        }
    }
}
