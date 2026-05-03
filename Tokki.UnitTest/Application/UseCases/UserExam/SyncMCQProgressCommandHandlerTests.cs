using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Commands.SyncMCQProgress;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class SyncMCQProgressCommandHandlerTests
    {
        private static SyncMCQProgressCommandHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new SyncMCQProgressCommandHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static SyncMCQProgressCommand BuildCommand(
            string userId = "USER-001",
            List<MCQAnswerDto>? answers = null)
        {
            return new SyncMCQProgressCommand
            {
                UserId  = userId,
                Answers = answers ?? new List<MCQAnswerDto>
                {
                    new MCQAnswerDto { UserQuestionId = "UEA-001", SelectedOptionId = "OPT-A" }
                }
            };
        }

        private static UserExamAnswer BuildMcqAnswer(
            string id = "UEA-001",
            string userId = "USER-001",
            UserExamStatus status = UserExamStatus.InProgress,
            string? selectedOpt = null)
        {
            return new UserExamAnswer
            {
                UserExamAnswerId = id,
                SelectedOptionId = selectedOpt,
                IsCorrect        = false,
                Question         = new QuestionBank
                {
                    QuestionBankId  = "QB-001",
                    Content         = "Test question",
                    QuestionOptions = new List<QuestionOption>
                    {
                        new QuestionOption { OptionId = "OPT-A", IsCorrect = true  },
                        new QuestionOption { OptionId = "OPT-B", IsCorrect = false }
                    }
                },
                UserExam = new Domain.Entities.UserExam
                {
                    UserId = userId,
                    Status = status
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // SyncMCQProgress_01 | N | Empty answers list → 200 with no DB write
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyAnswers_ShouldReturnSuccessWithoutDbCall()
        {
            // Arrange
            var repo    = new Mock<IUserExamRepository>();
            var handler = CreateHandler(repo);
            var command = new SyncMCQProgressCommand { UserId = "USER-001", Answers = new List<MCQAnswerDto>() };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.GetMCQAnswersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Never);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync MCQ", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgress",
                TestCaseID        = "SyncMCQProgress_01",
                Description       = "Empty answers list → immediate success, no DB lookup",
                ExpectedResult    = "IsSuccess=true, no DB call",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Answers=empty list" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SyncMCQProgress_02 | A | No matching MCQ records found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoMatchingMCQAnswers_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetMCQAnswersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserExamAnswer>());
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync MCQ", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgress",
                TestCaseID        = "SyncMCQProgress_02",
                Description       = "No MCQ answers found with given IDs → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetMCQAnswersByIdsAsync returns empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SyncMCQProgress_03 | A | UserId mismatch → 403
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserIdMismatch_ShouldReturn403()
        {
            // Arrange
            var answer = BuildMcqAnswer(userId: "USER-OWNER");
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetMCQAnswersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserExamAnswer> { answer });
            var handler  = CreateHandler(repo);
            var command  = BuildCommand(userId: "HACKER");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync MCQ", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgress",
                TestCaseID        = "SyncMCQProgress_03",
                Description       = "UserId does not match the exam owner → 403",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session.UserId != Request.UserId" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SyncMCQProgress_04 | A | Exam not InProgress → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamNotInProgress_ShouldReturn400()
        {
            // Arrange
            var answer = BuildMcqAnswer(status: UserExamStatus.Completed);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetMCQAnswersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserExamAnswer> { answer });
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync MCQ", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgress",
                TestCaseID        = "SyncMCQProgress_04",
                Description       = "Exam status is Completed, not InProgress → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Completed" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SyncMCQProgress_05 | N | Valid sync, answer changed → SaveChanges called
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSyncWithChange_ShouldSaveChanges()
        {
            // Arrange
            var answer = BuildMcqAnswer(selectedOpt: "OPT-B"); // previously OPT-B, syncing OPT-A
            var repo   = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetMCQAnswersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserExamAnswer> { answer });
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(BuildCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync MCQ", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgress",
                TestCaseID        = "SyncMCQProgress_05",
                Description       = "Answer changed, isModified=true → SaveChangesAsync called once",
                ExpectedResult    = "IsSuccess=true, SaveChanges Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SelectedOptionId changed", "IsModified=true" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SyncMCQProgress_06 | N | Same selected option → SaveChanges NOT called
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SameAnswer_ShouldNotCallSaveChanges()
        {
            // Arrange — currently selected OPT-A, syncing OPT-A → no change
            var answer = BuildMcqAnswer(selectedOpt: "OPT-A");
            var repo   = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetMCQAnswersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserExamAnswer> { answer });
            var handler = CreateHandler(repo);
            var command = BuildCommand(answers: new List<MCQAnswerDto>
            {
                new MCQAnswerDto { UserQuestionId = "UEA-001", SelectedOptionId = "OPT-A" }
            });

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Excel Log
            QACollector.LogTestCase("UserExam - Sync MCQ", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgress",
                TestCaseID        = "SyncMCQProgress_06",
                Description       = "SelectedOptionId unchanged → isModified=false → SaveChanges not called",
                ExpectedResult    = "IsSuccess=true, SaveChanges Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Same SelectedOptionId as before", "IsModified=false" }
            });
        }
    }
}
