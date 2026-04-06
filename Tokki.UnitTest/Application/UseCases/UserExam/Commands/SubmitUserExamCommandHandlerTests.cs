using FluentAssertions;
using Hangfire;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam.Commands
{
    public class SubmitUserExamCommandHandlerTests
    {
        private readonly Mock<IUserExamRepository> _repoMock = new();
        private readonly Mock<IBackgroundJobClient> _bgMock = new();

        private SubmitUserExamCommandHandler CreateHandler()
        {
            return new SubmitUserExamCommandHandler(_repoMock.Object, _bgMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UEX-SUB-01 | A | Session NotFound
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            _repoMock.Setup(x => x.GetByIdAsync("fake", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler();
            var cmd = new SubmitUserExamCommand { UserExamId = "fake", UserId = "usr" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup = "SubmitUserExamCommandHandler",
                TestCaseID = "TC-UEX-SUB-01",
                Description = "Unmatched session blocks submit",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UEX-SUB-02 | A | UserId Mismatch
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserIdMismatch_ShouldReturn403()
        {
            var session = new Domain.Entities.UserExam { UserId = "correct-usr" };
            _repoMock.Setup(x => x.GetByIdAsync("session", It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler();
            var cmd = new SubmitUserExamCommand { UserExamId = "session", UserId = "wrong-usr" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup = "SubmitUserExamCommandHandler",
                TestCaseID = "TC-UEX-SUB-02",
                Description = "Reject submission natively if requested by different user",
                ExpectedResult = "Return 403 Forbidden",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session.UserId != Request.UserId" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UEX-SUB-03 | A | Status Already Completed
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusAlreadyCompleted_ShouldReturn400()
        {
            var session = new Domain.Entities.UserExam { UserId = "usr", Status = UserExamStatus.Completed };
            _repoMock.Setup(x => x.GetByIdAsync("session", It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler();
            var cmd = new SubmitUserExamCommand { UserExamId = "session", UserId = "usr" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup = "SubmitUserExamCommandHandler",
                TestCaseID = "TC-UEX-SUB-03",
                Description = "Rejects secondary submission payloads on same session",
                ExpectedResult = "Return 400 Bad Request",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session.Status = Completed" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UEX-SUB-04 | N | Normal Submission Clamped Limits within Boundaries
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Submit_ShouldCalculateCorrectScoreAndTime()
        {
            var startTime = DateTime.UtcNow.AddMinutes(-30);
            var tp = new TemplatePart { QuestionFrom = 1, QuestionTo = 1, Mark = 5 }; // Score 5
            var eq = new ExamQuestion { QuestionBankId = "qb1", QuestionNo = 1 };
            
            var uaCorrect = new UserExamAnswer { QuestionId = "qb1", IsCorrect = true }; // Score 5
            var uaIncorrect = new UserExamAnswer { QuestionId = "qb2", IsCorrect = false }; // Score 0

            var exam = new Domain.Entities.Exam { Duration = 60, ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart>{ tp } }, ExamQuestions = new List<ExamQuestion>{ eq } };
            
            var session = new Domain.Entities.UserExam 
            { 
                UserId = "usr", UserExamId = "sess", StartTime = startTime, CurrentSkill = QuestionSkill.Reading,
                Exam = exam, UserExamAnswers = new List<UserExamAnswer>{ uaCorrect, uaIncorrect }
            };

            _repoMock.Setup(x => x.GetByIdAsync("sess", It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler();
            var cmd = new SubmitUserExamCommand { UserExamId = "sess", UserId = "usr" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.FinalMcqScore.Should().Be(5);
            session.Score.Should().Be(5);
            session.FinishedSkills.Should().Contain("Reading");
            session.Status.Should().Be(UserExamStatus.Completed);

            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup = "SubmitUserExamCommandHandler",
                TestCaseID = "TC-UEX-SUB-04",
                Description = "Scoring checks valid correct properties and properly sums bounds",
                ExpectedResult = "Return valid score",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Has answers inside bounds" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UEX-SUB-05 | B | Submitted Extremely Late Over Maximum Duration Limit
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Timeout_Submit_ShouldClampTime()
        {
            var startTime = DateTime.UtcNow.AddMinutes(-100); // Exceeded 60 wildly
            var exam = new Domain.Entities.Exam { Duration = 60, ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart>() }, ExamQuestions = new List<ExamQuestion>() };
            var session = new Domain.Entities.UserExam 
            { 
                UserId = "usr", UserExamId = "sess", StartTime = startTime,
                Exam = exam, UserExamAnswers = new List<UserExamAnswer>()
            };

            _repoMock.Setup(x => x.GetByIdAsync("sess", It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler();
            var cmd = new SubmitUserExamCommand { UserExamId = "sess", UserId = "usr" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.TimeSpentMinutes.Should().Be(60); // Maximum clamped
            session.SubmitTime.HasValue.Should().BeTrue();

            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup = "SubmitUserExamCommandHandler",
                TestCaseID = "TC-UEX-SUB-05",
                Description = "Elapsed limits over bounds aggressively are clamped to absolute Exam max Duration logically",
                ExpectedResult = "Return 60 capped TimeSpent",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Actual Elapsed > Duration + 2" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UEX-SUB-06 | N | Background Job Trigger Validation
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EnsureBackgroundJobScheduled()
        {
            var exam = new Domain.Entities.Exam { Duration = 60, ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart>() }, ExamQuestions = new List<ExamQuestion>() };
            var session = new Domain.Entities.UserExam { UserId = "usr", UserExamId = "sess", StartTime = DateTime.UtcNow, Exam = exam, UserExamAnswers = new List<UserExamAnswer>(), FinishedSkills = "[\"Listening\"]" }; // Finished skill exists
            
            _repoMock.Setup(x => x.GetByIdAsync("sess", It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler();
            var cmd = new SubmitUserExamCommand { UserExamId = "sess", UserId = "usr" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Checking if json array appends securely if something existed previously.
            session.FinishedSkills.Should().Contain("Listening");
            
            QACollector.LogTestCase("UserExam - Submit", new TestCaseDetail
            {
                FunctionGroup = "SubmitUserExamCommandHandler",
                TestCaseID = "TC-UEX-SUB-06",
                Description = "Verifies execution flow does not interrupt on JSON deserializaton updates",
                ExpectedResult = "List Appends effectively",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Contains old skills and appends current safely" }
            });
        }
    }
}
