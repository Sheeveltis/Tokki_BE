using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamReview;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetUserExamReviewQueryHandlerTests
    {
        private static GetUserExamReviewQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetUserExamReviewQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static Domain.Entities.UserExam BuildSession(
            string id = "UE-001",
            UserExamStatus status = UserExamStatus.Completed)
        {
            return new Domain.Entities.UserExam
            {
                UserExamId  = id,
                Status      = status,
                Score       = 5,
                StartTime   = DateTime.UtcNow.AddHours(-1),
                SubmitTime  = DateTime.UtcNow.AddMinutes(-30),
                Exam = new Domain.Entities.Exam
                {
                    Title = "TOPIK I 2024",
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 5, Mark = 1 }
                        }
                    }
                },
                UserExamAnswers = new List<UserExamAnswer>
                {
                    new UserExamAnswer
                    {
                        UserExamAnswerId = "ANS-001",
                        OrderIndex       = 1,
                        QuestionId       = "QB-001",
                        IsCorrect        = true,
                        SelectedOptionId = "OPT-A",
                        Question = new QuestionBank
                        {
                            Content         = "Question text",
                            Explanation     = "Explanation",
                            QuestionOptions = new List<QuestionOption>
                            {
                                new QuestionOption { OptionId = "OPT-A", Content = "A", IsCorrect = true },
                                new QuestionOption { OptionId = "OPT-B", Content = "B", IsCorrect = false }
                            }
                        }
                    }
                },
                UserExamWritingAnswers = new List<UserExamWritingAnswer>()
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-01 | A | Session not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamReviewQuery("INVALID"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-01",
                Description       = "UserExamId not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetReviewByIdAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-02 | A | Session still InProgress → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionInProgress_ShouldReturn400()
        {
            // Arrange
            var session = BuildSession(status: UserExamStatus.InProgress);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamReviewQuery("UE-001"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-02",
                Description       = "Exam not submitted → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=InProgress" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-03 | N | Happy path → 200 with ExamTitle and Questions
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidReview_ShouldReturn200WithExamTitleAndQuestions()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamReviewQuery("UE-001"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.ExamTitle.Should().Be("TOPIK I 2024");
            result.Data.Questions.Should().HaveCount(1);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-03",
                Description       = "Valid completed exam → 200 with ExamTitle and 1 question",
                ExpectedResult    = "IsSuccess=true, ExamTitle=TOPIK I 2024, Questions.Count=1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Completed session with 1 MCQ answer" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-04 | N | Questions ordered by OrderIndex
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ReviewQuestions_ShouldBeOrderedByIndex()
        {
            // Arrange
            var session = BuildSession();
            session.UserExamAnswers.Add(new UserExamAnswer
            {
                UserExamAnswerId = "ANS-002",
                OrderIndex       = 3,
                QuestionId       = "QB-002",
                IsCorrect        = false,
                Question         = new QuestionBank
                {
                    Content = "Q3", QuestionOptions = new List<QuestionOption>()
                }
            });
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamReviewQuery("UE-001"), CancellationToken.None);

            // Assert
            result.Data!.Questions[0].OrderIndex.Should().BeLessThan(result.Data.Questions[1].OrderIndex);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-04",
                Description       = "Review questions ordered by OrderIndex ascending",
                ExpectedResult    = "Questions[0].OrderIndex < Questions[1].OrderIndex",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 answers with OrderIndex 1 and 3" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-05 | N | TotalScore from session mapped correctly
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidReview_ShouldMapTotalScoreFromSession()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetUserExamReviewQuery("UE-001"), CancellationToken.None);

            // Assert
            result.Data!.TotalScore.Should().Be(5);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-05",
                Description       = "TotalScore mapped from session.Score",
                ExpectedResult    = "TotalScore=5",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session.Score=5" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB failure"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(new GetUserExamReviewQuery("UE-001"), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB failure");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Exception with 'DB failure'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetReviewByIdAsync throws Exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-07 | N | Missing template part mapping returns Unknown skill
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UnknownPartIndex_ShouldReturnUnknownSkillAndZeroScore()
        {
            var session = BuildSession();
            session.UserExamAnswers.Clear();
            session.UserExamAnswers.Add(new UserExamAnswer
            {
                OrderIndex = 99, // out of range of 1-5 template part
                Question = new QuestionBank { Content = "Q99", QuestionOptions = new List<QuestionOption>() }
            });
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler(repo);

            var result = await handler.Handle(new GetUserExamReviewQuery("UE-001"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Questions[0].Skill.Should().Be("Unknown");
            result.Data.Questions[0].QuestionMaxScore.Should().Be(0);

            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-07",
                Description       = "OrderIndex does not map to any template part",
                ExpectedResult    = "Skill=Unknown and QuestionMaxScore=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "part == null branch hit" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GURV-08 | N | Writing part mapping to review questions
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingAnswers_ShouldMapToReviewQuestions()
        {
            var session = BuildSession();
            session.UserExamAnswers.Clear();
            session.UserExamWritingAnswers.Add(new UserExamWritingAnswer
            {
                OrderIndex = 2,
                Score = 8,
                AnswerContent = "Writing ans",
                AiAnalysisJson = "{\"score\": 8}",
                Question = new QuestionBank { Content = "W1" }
            });
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReviewByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler(repo);

            var result = await handler.Handle(new GetUserExamReviewQuery("UE-001"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Questions.Should().HaveCount(1);
            result.Data.Questions[0].Skill.Should().Be("Writing");
            result.Data.Questions[0].WritingScore.Should().Be(8);
            result.Data.Questions[0].AiAnalysisJson.Should().Be("{\"score\": 8}");

            QACollector.LogTestCase("UserExam - Get Review", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExamReview",
                TestCaseID        = "TC-GURV-08",
                Description       = "Writing answers mapped correctly",
                ExpectedResult    = "Skill=Writing, Properties correctly set",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserExamWritingAnswers iteration" }
            });
        }
    }
}
