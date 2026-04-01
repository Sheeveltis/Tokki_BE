using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetUserExamResultQueryHandlerTests
    {
        private static GetUserExamResultQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetUserExamResultQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static Domain.Entities.UserExam BuildCompletedSession(
            string id = "UE-001",
            UserExamStatus status = UserExamStatus.Completed)
        {
            return new Domain.Entities.UserExam
            {
                UserExamId = id,
                Status     = status,
                Score      = 10,
                User       = new Account { FullName = "Nguyen Van A" },
                Exam = new Domain.Entities.Exam
                {
                    Title    = "TOPIK Test 2024",
                    Duration = 60,
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1,  QuestionTo = 10, Mark = 1 },
                            new TemplatePart { Skill = QuestionSkill.Reading,   QuestionFrom = 11, QuestionTo = 20, Mark = 1 }
                        }
                    }
                },
                UserExamAnswers = new List<UserExamAnswer>
                {
                    new UserExamAnswer { OrderIndex = 1,  IsCorrect = true  },
                    new UserExamAnswer { OrderIndex = 11, IsCorrect = false }
                },
                UserExamWritingAnswers = new List<UserExamWritingAnswer>()
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURS-01 | A | Session not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetResultWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamResultQuery { UserExamId = "INVALID" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Result", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamResult",
                TestCaseID        = "TC-GURS-01",
                Description       = "UserExamId not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetResultWithDetailsAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURS-02 | A | Session still InProgress → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotSubmitted_ShouldReturn400()
        {
            // Arrange
            var session = BuildCompletedSession(status: UserExamStatus.InProgress);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetResultWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamResultQuery { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Result", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamResult",
                TestCaseID        = "TC-GURS-02",
                Description       = "Exam not submitted → 400 cannot view result",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = InProgress" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURS-03 | A | Missing exam template structure → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingTemplateParts_ShouldReturn400()
        {
            // Arrange
            var session = new Domain.Entities.UserExam
            {
                UserExamId = "UE-001",
                Status     = UserExamStatus.Completed,
                User       = new Account { FullName = "Test" },
                Exam = new Domain.Entities.Exam
                {
                    Title        = "T",
                    ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart>() }
                },
                UserExamAnswers       = new List<UserExamAnswer>(),
                UserExamWritingAnswers = new List<UserExamWritingAnswer>()
            };
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetResultWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamResultQuery { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Result", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamResult",
                TestCaseID        = "TC-GURS-03",
                Description       = "TemplateParts is empty → 400 corrupted structure",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateParts=empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURS-04 | N | Happy path → 200 with UserName and ExamTitle
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCompleted_ShouldReturn200WithUserAndExamTitle()
        {
            // Arrange
            var session = BuildCompletedSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetResultWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamResultQuery { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.UserName.Should().Be("Nguyen Van A");
            result.Data.ExamTitle.Should().Be("TOPIK Test 2024");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Result", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamResult",
                TestCaseID        = "TC-GURS-04",
                Description       = "Valid completed exam → 200 with UserName and ExamTitle",
                ExpectedResult    = "IsSuccess=true, UserName=Nguyen Van A",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=Completed", "User and Exam loaded" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURS-05 | N | Score calculation correct (1 listening correct)
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OneListeningCorrect_ShouldHaveCorrectListeningScore()
        {
            // Arrange
            var session = BuildCompletedSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetResultWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamResultQuery { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Listening.CorrectAnswers.Should().Be(1);
            result.Data.Reading.CorrectAnswers.Should().Be(0);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Result", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamResult",
                TestCaseID        = "TC-GURS-05",
                Description       = "1 listening correct, 0 reading correct → scores mapped properly",
                ExpectedResult    = "Listening.CorrectAnswers=1, Reading.CorrectAnswers=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 Listening correct, 1 Reading incorrect" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURS-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetResultWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(new GetUserExamResultQuery { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Result", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamResult",
                TestCaseID        = "TC-GURS-06",
                Description       = "Repository exception → propagates",
                ExpectedResult    = "Exception with 'DB error'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetResultWithDetailsAsync throws Exception" }
            });
        }
    }
}
