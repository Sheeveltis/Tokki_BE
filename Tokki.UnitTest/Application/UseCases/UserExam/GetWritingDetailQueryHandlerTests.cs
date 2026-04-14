using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Queries.GetWritingDetail;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetWritingDetailQueryHandlerTests
    {
        private static GetWritingDetailQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetWritingDetailQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static GetWritingDetailQuery MakeQuery(string id = "UE-001")
            => new GetWritingDetailQuery { UserExamId = id };

        private static Domain.Entities.UserExam BuildCompletedWritingSession()
        {
            return new Domain.Entities.UserExam
            {
                UserExamId  = "UE-001",
                Status      = UserExamStatus.Completed,
                UserExamAnswers = new List<UserExamAnswer>(),
                UserExamWritingAnswers = new List<UserExamWritingAnswer>
                {
                    new UserExamWritingAnswer
                    {
                        UserExamWritingAnswerId = "UWA-001",
                        OrderIndex              = 51,
                        AnswerContent           = "My essay content",
                        WordCount               = 17,
                        Score                   = 8,
                        AiAnalysisJson          = null,
                        GradedAt                = DateTime.UtcNow,
                        Question = new QuestionBank
                        {
                            Content = "Write about Korean culture", MediaUrl = null, Passage = null
                        }
                    }
                },
                Exam = new Domain.Entities.Exam
                {
                    Title = "TOPIK II",
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Writing, QuestionFrom = 51, QuestionTo = 54, Mark = 10 }
                        }
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-01 | A | Session not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery("INVALID"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-01",
                Description       = "Session not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetWritingDetailAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-02 | A | Session still InProgress → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionInProgress_ShouldReturn400()
        {
            // Arrange
            var session = BuildCompletedWritingSession();
            session.Status = UserExamStatus.InProgress;
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-02",
                Description       = "Exam not submitted → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=InProgress" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-03 | A | Empty template parts → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyTemplateParts_ShouldReturn400()
        {
            // Arrange
            var session = BuildCompletedWritingSession();
            session.Exam.ExamTemplate.TemplateParts = new List<TemplatePart>();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-03",
                Description       = "No template parts → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateParts=empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-04 | N | Happy path → 200 with TotalQuestions and MaxScore
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSession_ShouldReturn200WithMaxScore()
        {
            // Arrange
            var session = BuildCompletedWritingSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalQuestions.Should().Be(4);  // 51-54
            result.Data.MaxScore.Should().Be(40.0);       // 4 × 10

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-04",
                Description       = "Writing Q51-Q54, Mark=10 → TotalQuestions=4, MaxScore=40",
                ExpectedResult    = "IsSuccess=true, TotalQuestions=4, MaxScore=40",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Writing Q51-Q54, Mark=10" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-05 | N | Graded answer score summed correctly
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_GradedAnswer_ShouldSumScoreCorrectly()
        {
            // Arrange
            var session = BuildCompletedWritingSession(); // Score=8
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.Data!.Score.Should().Be(8.0);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-05",
                Description       = "Answer has Score=8 → response.Score=8",
                ExpectedResult    = "Score=8.0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "WritingAnswer.Score=8" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Exception with 'DB error'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetWritingDetailAsync throws Exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-07 | N | Parse AI Analysis logic JSON successful and JSON parsing failure
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AiAnalysisParsing_ShouldCatchAndAssignProperly()
        {
            // Arrange
            var session = BuildCompletedWritingSession();
            session.UserExamWritingAnswers.Clear();
            session.UserExamWritingAnswers.Add(new UserExamWritingAnswer
            {
                OrderIndex = 51,
                AiAnalysisJson = "{\"ok\": true}", // valid json
                Question = new QuestionBank { Content = "Q51" }
            });
            session.UserExamWritingAnswers.Add(new UserExamWritingAnswer
            {
                OrderIndex = 52,
                AiAnalysisJson = "{invalid json", // invalid json
                Question = new QuestionBank { Content = "Q52" }
            });

            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetWritingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var questions = result.Data!.QuestionGroups[0].Questions;
            questions.Should().HaveCount(2);

            // First: valid parse
            var ai1Str = System.Text.Json.JsonSerializer.Serialize(questions[0].AiAnalysis);
            ai1Str.Should().Contain("\"ok\":true");
            
            // Second: parse exception fallback
            var ai2Str = System.Text.Json.JsonSerializer.Serialize(questions[1].AiAnalysis);
            ai2Str.Should().Contain("\"isParseError\":true");

            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-07",
                Description       = "AiAnalysisJson deserialization try/catch behavior mapping",
                ExpectedResult    = "Parsed object for valid schema, parseError object for invalid JSON string",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch block in AI Analysis Deserialization" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GWRD-08 | N | Null ExamTemplate and MediaType parsing
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MediaTypeAndNullTemplate_ShouldProcessProperly()
        {
            // Null template branches
            var nullSession = BuildCompletedWritingSession();
            nullSession.Exam.ExamTemplate = null;
            var repo1 = new Mock<IUserExamRepository>();
            repo1.Setup(x => x.GetWritingDetailAsync("NULL_TEMPLATE", It.IsAny<CancellationToken>())).ReturnsAsync(nullSession);
            var result1 = await CreateHandler(repo1).Handle(new GetWritingDetailQuery { UserExamId = "NULL_TEMPLATE" }, CancellationToken.None);
            result1.IsSuccess.Should().BeFalse();

            // Media mapping
            var session = BuildCompletedWritingSession();
            session.UserExamWritingAnswers.First().Question.MediaUrl = "vid.mp4"; // unknown
            var repo2 = new Mock<IUserExamRepository>();
            repo2.Setup(x => x.GetWritingDetailAsync("MEDIA_TEST", It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var result2 = await CreateHandler(repo2).Handle(new GetWritingDetailQuery { UserExamId = "MEDIA_TEST" }, CancellationToken.None);
            
            result2.IsSuccess.Should().BeTrue();
            result2.Data!.QuestionGroups[0].SharedMediaType.Should().Be("Unknown");

            QACollector.LogTestCase("UserExam - Get Writing Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetWritingDetail",
                TestCaseID        = "TC-GWRD-08",
                Description       = "Null ExamTemplate fails, GetMediaType maps unknown files properly",
                ExpectedResult    = "Fail on null template, Unknown SharedMediaType for .mp4",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "null ExamTemplate", "MediaUrl is mp4 -> Unknown branch" }
            });
        }
    }
}
