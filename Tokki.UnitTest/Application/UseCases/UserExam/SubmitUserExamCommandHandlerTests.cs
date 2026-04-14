using FluentAssertions;
using Hangfire;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class SubmitUserExamCommandHandlerTests
    {
        private static SubmitUserExamCommandHandler CreateHandler(
            Mock<IUserExamRepository>? repo = null,
            Mock<IBackgroundJobClient>? jobs = null)
            => new SubmitUserExamCommandHandler(
                (repo ?? new Mock<IUserExamRepository>()).Object,
                (jobs ?? MockBackgroundJobClient.GetMock()).Object);

        private static Domain.Entities.UserExam BuildSession(
            string userId       = "USER-001",
            string userExamId   = "UE-001",
            UserExamStatus status = UserExamStatus.InProgress,
            int durationMin     = 60)
        {
            return new Domain.Entities.UserExam
            {
                UserExamId           = userExamId,
                UserId               = userId,
                Status               = status,
                StartTime            = DateTime.UtcNow.AddMinutes(-30),
                CurrentSkill         = QuestionSkill.Reading,
                FinishedSkills       = "[]",
                UserExamAnswers      = new List<UserExamAnswer>
                {
                    new UserExamAnswer { UserExamAnswerId = "ANS-001", IsCorrect = true  },
                    new UserExamAnswer { UserExamAnswerId = "ANS-002", IsCorrect = false }
                },
                UserExamWritingAnswers = new List<UserExamWritingAnswer>(),
                Exam = new Domain.Entities.Exam
                {
                    ExamId   = "EXAM-001",
                    Duration = durationMin,
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1,  QuestionTo = 20 },
                            new TemplatePart { Skill = QuestionSkill.Reading,   QuestionFrom = 21, QuestionTo = 40 }
                        }
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SUEX-01 | A | Session not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new SubmitUserExamCommand { UserId = "USER-001", UserExamId = "INVALID" },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitUserExam",
                TestCaseID        = "TC-SUEX-01",
                Description       = "Submit with non-existent UserExamId → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SUEX-02 | A | UserId mismatch → 403 Forbidden
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserIdMismatch_ShouldReturn403()
        {
            // Arrange
            var session = BuildSession(userId: "USER-001");
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act — wrong user submitting
            var result = await handler.Handle(
                new SubmitUserExamCommand { UserId = "USER-HACKER", UserExamId = "UE-001" },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            // Excel Log
            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitUserExam",
                TestCaseID        = "TC-SUEX-02",
                Description       = "Different user attempts to submit another user's exam → 403",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session.UserId != Request.UserId" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SUEX-03 | A | Exam already Completed → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamAlreadyCompleted_ShouldReturn400()
        {
            // Arrange
            var session = BuildSession(status: UserExamStatus.Completed);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new SubmitUserExamCommand { UserId = "USER-001", UserExamId = "UE-001" },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitUserExam",
                TestCaseID        = "TC-SUEX-03",
                Description       = "Exam already submitted (Completed) → 400 Bad Request",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Completed" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SUEX-04 | N | Happy path: valid submit → 200, score calculated
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSubmit_ShouldReturn200WithScore()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var jobs = MockBackgroundJobClient.GetMock();
            var handler = CreateHandler(repo, jobs);

            // Act
            var result = await handler.Handle(
                new SubmitUserExamCommand { UserId = "USER-001", UserExamId = "UE-001" },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.FinalMcqScore.Should().Be(1); // 1 correct answer out of 2

            // Excel Log
            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitUserExam",
                TestCaseID        = "TC-SUEX-04",
                Description       = "Valid submit with 1 correct answer → 200, FinalMcqScore=1",
                ExpectedResult    = "IsSuccess=true, FinalMcqScore=1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid session", "1 correct answer", "MCQ score calculated" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SUEX-05 | N | SaveChangesAsync called once on success
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSubmit_ShouldCallSaveChangesOnce()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(
                new SubmitUserExamCommand { UserId = "USER-001", UserExamId = "UE-001" },
                CancellationToken.None);

            // Assert
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitUserExam",
                TestCaseID        = "TC-SUEX-05",
                Description       = "SaveChangesAsync called exactly once when exam is submitted",
                ExpectedResult    = "SaveChangesAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid submission", "DB commit verified" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-SUEX-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(
                new SubmitUserExamCommand { UserId = "USER-001", UserExamId = "UE-001" },
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup     = "SubmitUserExam",
                TestCaseID        = "TC-SUEX-06",
                Description       = "Repository throws exception → exception propagates up",
                ExpectedResult    = "Exception thrown with 'DB error'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws Exception" }
            });
        }
    }
}
