using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Queries.GetInProgressExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetInProgressExamQueryHandlerTests
    {
        private static GetInProgressExamQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetInProgressExamQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static GetInProgressExamQuery MakeQuery(string id = "UE-001")
            => new GetInProgressExamQuery { UserExamId = id };

        private static Domain.Entities.UserExam BuildSession(string id = "UE-001")
        {
            return new Domain.Entities.UserExam
            {
                UserExamId           = id,
                UserId               = "USER-001",
                ExamId               = "EXAM-001",
                Status               = UserExamStatus.InProgress,
                StartTime            = DateTime.UtcNow.AddMinutes(-10),
                CurrentSkill         = QuestionSkill.Listening,
                CurrentSkillStartTime = DateTime.UtcNow.AddMinutes(-10),
                FinishedSkills       = "[]",
                UserExamAnswers      = new List<UserExamAnswer>(),
                UserExamWritingAnswers = new List<UserExamWritingAnswer>(),
                Exam = new Domain.Entities.Exam
                {
                    ExamId    = "EXAM-001",
                    Title     = "TOPIK I",
                    Duration  = 60,
                    SkillDurations = "{\"Listening\":30,\"Reading\":30}",
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 5 }
                        }
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-01 | A | Session not found → 404
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
            var result = await handler.Handle(MakeQuery("INVALID"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-01",
                Description       = "UserExamId not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-02 | N | Session found → 200 with UserExamId and Title
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionFound_ShouldReturn200WithSessionData()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.UserExamId.Should().Be("UE-001");
            result.Data.Title.Should().Be("TOPIK I");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-02",
                Description       = "Valid session → 200 with UserExamId and Title mapped",
                ExpectedResult    = "IsSuccess=true, UserExamId=UE-001, Title=TOPIK I",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Session found and mapped" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-03 | N | ExamId forwarded correctly to repo
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ShouldCallGetByIdWithCorrectId()
        {
            // Arrange
            var session = BuildSession("UE-999");
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync("UE-999", It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(MakeQuery("UE-999"), CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetByIdAsync("UE-999", It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-03",
                Description       = "GetByIdAsync called once with correct UserExamId",
                ExpectedResult    = "Times.Once with 'UE-999'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserExamId forwarded" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-04 | N | Duration and TotalQuestions populated
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSession_ShouldMapDurationAndTotalQuestions()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.Data!.Duration.Should().Be(60); // maps session.Exam.Duration directly
            result.Data.TotalQuestions.Should().Be(0); // questionsMap.Count == 0 (no seeded answers)

            // Excel Log
            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-04",
                Description       = "Duration and TotalQuestions mapped from session",
                ExpectedResult    = "Duration=60, TotalQuestions=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exam.Duration=60, no seeded answers" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-05 | N | CurrentSkill mapped to response
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSession_ShouldMapCurrentSkill()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.Data!.CurrentSkill.Should().Be("Listening");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-05",
                Description       = "CurrentSkill mapped correctly (Listening)",
                ExpectedResult    = "CurrentSkill='Listening'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CurrentSkill=Listening in session" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("timeout"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("timeout");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Exception with 'timeout'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws Exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-07 | N | Branch: finished skill sets remaining to 0
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FinishedSkillAndNotStartedSkill_ShouldCalculateSkillRemainingCorrectly()
        {
            var session = BuildSession();
            session.FinishedSkills = "[\"Listening\"]"; // Already finished
            session.CurrentSkill = QuestionSkill.Reading;
            session.Exam.SkillDurations = "{\"Listening\":30,\"Reading\":30,\"Writing\":40}"; // 30 mins each
            
            // Add a writing template part so it gets included
            session.Exam.ExamTemplate.TemplateParts.Add(new TemplatePart { Skill = QuestionSkill.Reading, QuestionFrom = 6, QuestionTo = 10 });
            session.Exam.ExamTemplate.TemplateParts.Add(new TemplatePart { Skill = QuestionSkill.Writing, QuestionFrom = 11, QuestionTo = 12 });

            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler(repo);

            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Listening finished -> 0
            result.Data!.SkillTimeRemaining["Listening"].Should().Be(0);
            // Writing not yet started -> 40 * 60 = 2400
            result.Data!.SkillTimeRemaining["Writing"].Should().Be(2400);

            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-07",
                Description       = "FinishedSkill has remaining 0, Not yet started skill has max remaining",
                ExpectedResult    = "Listening=0, Writing=2400",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "finishedList.Contains", "s != session.CurrentSkill" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GIPR-08 | N | Branch: MediaType parsing and Writing Answers grouping
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingAnswersAndMediaUrl_ShouldCalculateMediaTypeAndGroups()
        {
            var session = BuildSession();
            // Add a writing part
            session.Exam.ExamTemplate.TemplateParts.Clear();
            session.Exam.ExamTemplate.TemplateParts.Add(new TemplatePart { Skill = QuestionSkill.Writing, QuestionFrom = 1, QuestionTo = 2, PartTitle = "Writing Part" });
            
            // Add UserExamWritingAnswers
            var writingQ1 = new QuestionBank { Content = "W1", MediaUrl = "audio.mp3", Passage = new Passage { Content = "Shared", AudioUrl = "hello.mp3" } };
            var writingQ2 = new QuestionBank { Content = "W2", MediaUrl = "image.png", Passage = new Passage { Content = "Shared2", ImageUrl = "hello.png" } };
            
            session.UserExamWritingAnswers.Add(new UserExamWritingAnswer { OrderIndex = 1, Question = writingQ1, AnswerContent = "My ans" });
            session.UserExamWritingAnswers.Add(new UserExamWritingAnswer { OrderIndex = 2, Question = writingQ2, AnswerContent = "My ans 2" });

            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
            var handler = CreateHandler(repo);

            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var writingParts = result.Data!.Part.Writing;
            writingParts.Should().HaveCount(1);
            
            var groups = writingParts.First().QuestionGroups;
            groups.Should().HaveCount(2); // Diff media url and passage -> 2 groups
            groups[0].SharedMediaType.Should().Be("Audio");
            groups[1].SharedMediaType.Should().Be("Image");

            QACollector.LogTestCase("UserExam - Get In Progress", new TestCaseDetail
            {
                FunctionGroup     = "GetInProgressExam",
                TestCaseID        = "TC-GIPR-08",
                Description       = "Writing skill answers mapped and GetMediaType parses Audio/Image correctly",
                ExpectedResult    = "SharedMediaType=Audio and Image, Writing group populated",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "item.IsWriting=true", ".mp3 -> Audio, .png -> Image" }
            });
        }
    }
}
