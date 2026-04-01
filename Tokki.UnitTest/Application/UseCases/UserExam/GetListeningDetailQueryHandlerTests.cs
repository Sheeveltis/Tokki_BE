using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Queries.GetListeningDetail;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetListeningDetailQueryHandlerTests
    {
        private static GetListeningDetailQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetListeningDetailQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static GetListeningDetailQuery MakeQuery(string id = "UE-001")
            => new GetListeningDetailQuery { UserExamId = id };

        private static Domain.Entities.UserExam BuildCompletedSession()
        {
            return new Domain.Entities.UserExam
            {
                UserExamId  = "UE-001",
                Status      = UserExamStatus.Completed,
                UserExamAnswers = new List<UserExamAnswer>
                {
                    new UserExamAnswer
                    {
                        UserExamAnswerId = "ANS-001",
                        OrderIndex       = 1,
                        IsCorrect        = true,
                        SelectedOptionId = "OPT-A",
                        Question = new QuestionBank
                        {
                            Content = "Q1", MediaUrl = null, Passage = null,
                            QuestionOptions = new List<QuestionOption>
                            {
                                new QuestionOption { OptionId = "OPT-A", IsCorrect = true, KeyOption = "A", Content = "Answer A" }
                            }
                        }
                    }
                },
                UserExamWritingAnswers = new List<UserExamWritingAnswer>(),
                Exam = new Domain.Entities.Exam
                {
                    Title = "TOPIK I",
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 5, Mark = 1 }
                        }
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GLSD-01 | A | Session not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetListeningDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery("INVALID"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Listening Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetListeningDetail",
                TestCaseID        = "TC-GLSD-01",
                Description       = "Session not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetListeningDetailAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GLSD-02 | A | Session still InProgress → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionInProgress_ShouldReturn400()
        {
            // Arrange
            var session = BuildCompletedSession();
            session.Status = UserExamStatus.InProgress;
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetListeningDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Listening Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetListeningDetail",
                TestCaseID        = "TC-GLSD-02",
                Description       = "Exam not submitted → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=InProgress" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GLSD-03 | A | No listening template parts → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoListeningParts_ShouldReturn400()
        {
            // Arrange
            var session = BuildCompletedSession();
            session.Exam.ExamTemplate.TemplateParts = new List<TemplatePart>(); // no parts at all
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetListeningDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Listening Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetListeningDetail",
                TestCaseID        = "TC-GLSD-03",
                Description       = "No template parts for exam → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateParts is empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GLSD-04 | N | Happy path → 200 with TotalQuestions and MaxScore
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSession_ShouldReturn200WithQuestionCountAndMaxScore()
        {
            // Arrange
            var session = BuildCompletedSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetListeningDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalQuestions.Should().Be(5);
            result.Data.MaxScore.Should().Be(5.0);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Listening Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetListeningDetail",
                TestCaseID        = "TC-GLSD-04",
                Description       = "Part has 5 questions with Mark=1 → TotalQuestions=5, MaxScore=5",
                ExpectedResult    = "IsSuccess=true, TotalQuestions=5, MaxScore=5",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 ListeningPart, Q1-Q5, Mark=1" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GLSD-05 | N | Correct answer counted in Score
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OneCorrectAnswer_ShouldCountInScore()
        {
            // Arrange
            var session = BuildCompletedSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetListeningDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.Data!.CorrectAnswers.Should().Be(1);
            result.Data.Score.Should().Be(1.0);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Listening Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetListeningDetail",
                TestCaseID        = "TC-GLSD-05",
                Description       = "1 correct answer (IsCorrect=true) → CorrectAnswers=1, Score=1",
                ExpectedResult    = "CorrectAnswers=1, Score=1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 answer with IsCorrect=true, Mark=1" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GLSD-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetListeningDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB failure"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB failure");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Listening Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetListeningDetail",
                TestCaseID        = "TC-GLSD-06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Exception with 'DB failure'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetListeningDetailAsync throws Exception" }
            });
        }
    }
}
