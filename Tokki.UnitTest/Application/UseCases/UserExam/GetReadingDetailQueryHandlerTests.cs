using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Queries.GetReadingDetail;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetReadingDetailQueryHandlerTests
    {
        private static GetReadingDetailQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetReadingDetailQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static GetReadingDetailQuery MakeQuery(string id = "UE-001")
            => new GetReadingDetailQuery { UserExamId = id };

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
                        OrderIndex       = 21,
                        IsCorrect        = true,
                        SelectedOptionId = "OPT-A",
                        Question = new QuestionBank
                        {
                            Content = "Reading Q1", MediaUrl = null, Passage = null,
                            QuestionOptions = new List<QuestionOption>
                            {
                                new QuestionOption { OptionId = "OPT-A", IsCorrect = true, KeyOption = "A", Content = "A" }
                            }
                        }
                    }
                },
                UserExamWritingAnswers = new List<UserExamWritingAnswer>(),
                Exam = new Domain.Entities.Exam
                {
                    Title = "TOPIK II",
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 21, QuestionTo = 30, Mark = 2 }
                        }
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_01 | A | Session not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery("INVALID"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_01",
                Description       = "Session not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetReadingDetailAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_02 | A | Session still InProgress → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionInProgress_ShouldReturn400()
        {
            // Arrange
            var session = BuildCompletedSession();
            session.Status = UserExamStatus.InProgress;
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_02",
                Description       = "Exam not submitted → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=InProgress" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_03 | A | Empty template parts → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyTemplateParts_ShouldReturn400()
        {
            // Arrange
            var session = BuildCompletedSession();
            session.Exam.ExamTemplate.TemplateParts = new List<TemplatePart>();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_03",
                Description       = "No template parts → 400 corrupted structure",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateParts=empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_04 | N | Happy path → 200 with TotalQuestions and MaxScore
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSession_ShouldReturn200WithTotalQuestionsAndMaxScore()
        {
            // Arrange
            var session = BuildCompletedSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalQuestions.Should().Be(10);
            result.Data.MaxScore.Should().Be(20.0);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_04",
                Description       = "Reading part Q21-Q30, Mark=2 → TotalQuestions=10, MaxScore=20",
                ExpectedResult    = "IsSuccess=true, TotalQuestions=10, MaxScore=20",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Reading Q21-Q30, Mark=2" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_05 | N | 1 correct reading answer → Score=2 (mark*1)
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OneCorrectAnswer_ShouldApplyMarkToScore()
        {
            // Arrange
            var session = BuildCompletedSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.Data!.CorrectAnswers.Should().Be(1);
            result.Data.Score.Should().Be(2.0); // 1 correct × mark 2

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_05",
                Description       = "1 correct reading answer with Mark=2 → Score=2",
                ExpectedResult    = "CorrectAnswers=1, Score=2.0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCorrect=true, Mark=2" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("timeout"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("timeout");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Exception with 'timeout'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetReadingDetailAsync throws Exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_07 | N | Null ExamTemplate or incorrect answer false/null
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullExamTemplate_ShouldReturn400()
        {
            var session = BuildCompletedSession();
            session.Exam.ExamTemplate = null; // null template
            
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
            
            var handler = CreateHandler(repo);
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_07",
                Description       = "Null templateParts branch check",
                ExpectedResult    = "400 error",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session.Exam?.ExamTemplate?.TemplateParts is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetReadingDetail_08 | N | MediaType parsing branches
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MediaParsing_ShouldGroupCorrectly()
        {
            var session = BuildCompletedSession();
            var q1 = new QuestionBank { Content = "Q1", MediaUrl = "test.webp", QuestionOptions = new List<QuestionOption>() };
            var q2 = new QuestionBank { Content = "Q2", MediaUrl = "video.mp4", QuestionOptions = new List<QuestionOption>() };
            
            session.UserExamAnswers = new List<UserExamAnswer>
            {
                new UserExamAnswer { OrderIndex = 21, Question = q1, IsCorrect = false },
                new UserExamAnswer { OrderIndex = 22, Question = q2, IsCorrect = null }
            };
            
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetReadingDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
            
            var handler = CreateHandler(repo);
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Score.Should().Be(0); // none correct
            
            var groups = result.Data.QuestionGroups;
            groups.Should().HaveCount(2);
            groups[0].SharedMediaType.Should().Be("Image");
            groups[1].SharedMediaType.Should().Be("Unknown");
            
            QACollector.LogTestCase("UserExam - Get Reading Detail", new TestCaseDetail
            {
                FunctionGroup     = "GetReadingDetail",
                TestCaseID        = "GetReadingDetail_08",
                Description       = "Check GetMediaType for Image and Unknown extensions and IsCorrect branches",
                ExpectedResult    = "2 groups, SharedMediaType='Image' and 'Unknown', Score=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetMediaType image/unknown", "IsCorrect=false/null" }
            });
        }
    }
}
