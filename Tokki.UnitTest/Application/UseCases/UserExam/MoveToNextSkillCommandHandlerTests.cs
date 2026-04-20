using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Commands.MoveToNextSkill;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class MoveToNextSkillCommandHandlerTests
    {
        private static MoveToNextSkillCommandHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new MoveToNextSkillCommandHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static Domain.Entities.UserExam BuildSession(
            string id = "UE-001",
            UserExamStatus status = UserExamStatus.InProgress,
            QuestionSkill currentSkill = QuestionSkill.Listening,
            List<TemplatePart>? parts = null)
        {
            return new Domain.Entities.UserExam
            {
                UserExamId = id,
                UserId = "USER-001",
                Status = status,
                CurrentSkill = currentSkill,
                CurrentSkillStartTime = DateTime.UtcNow.AddMinutes(-5),
                FinishedSkills = "[]",
                Exam = new Domain.Entities.Exam
                {
                    ExamId = "EXAM-001",
                    ExamTemplate = new ExamTemplate
                    {
                        TemplateParts = parts ?? new List<TemplatePart>
                        {
                            new TemplatePart { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 20 },
                            new TemplatePart { Skill = QuestionSkill.Reading,   QuestionFrom = 21, QuestionTo = 40 },
                            new TemplatePart { Skill = QuestionSkill.Writing,   QuestionFrom = 41, QuestionTo = 42 }
                        }
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // MoveToNextSkill_01 | A | UserExamId not found → 404
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
            var result = await handler.Handle(new MoveToNextSkillCommand { UserExamId = "INVALID" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Move To Next Skill", new TestCaseDetail
            {
                FunctionGroup     = "MoveToNextSkill",
                TestCaseID        = "MoveToNextSkill_01",
                Description       = "UserExamId does not exist → 404 Not Found",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // MoveToNextSkill_02 | A | Exam already Completed → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionAlreadyCompleted_ShouldReturn400()
        {
            // Arrange
            var session = BuildSession(status: UserExamStatus.Completed);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new MoveToNextSkillCommand { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Move To Next Skill", new TestCaseDetail
            {
                FunctionGroup     = "MoveToNextSkill",
                TestCaseID        = "MoveToNextSkill_02",
                Description       = "Exam session is already Completed → 400 Bad Request",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Completed" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // MoveToNextSkill_03 | A | Already on last skill → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CurrentSkillIsLast_ShouldReturn400()
        {
            // Arrange — session is already at Writing (last skill)
            var session = BuildSession(currentSkill: QuestionSkill.Writing);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new MoveToNextSkillCommand { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Move To Next Skill", new TestCaseDetail
            {
                FunctionGroup     = "MoveToNextSkill",
                TestCaseID        = "MoveToNextSkill_03",
                Description       = "Current skill is the last skill (Writing) → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CurrentSkill = last in skillsInOrder" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // MoveToNextSkill_04 | N | Move Listening→Reading → 200, CurrentSkill updated
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MoveFromListeningToReading_ShouldReturn200()
        {
            // Arrange
            var session = BuildSession(currentSkill: QuestionSkill.Listening);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new MoveToNextSkillCommand { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();

            // Excel Log
            QACollector.LogTestCase("UserExam - Move To Next Skill", new TestCaseDetail
            {
                FunctionGroup     = "MoveToNextSkill",
                TestCaseID        = "MoveToNextSkill_04",
                Description       = "Move from Listening to Reading → 200 success",
                ExpectedResult    = "IsSuccess=true, StatusCode=200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CurrentSkill=Listening, next=Reading" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // MoveToNextSkill_05 | N | SaveChangesAsync called once on success
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidMove_ShouldCallSaveChangesOnce()
        {
            // Arrange
            var session = BuildSession(currentSkill: QuestionSkill.Listening);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(new MoveToNextSkillCommand { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Move To Next Skill", new TestCaseDetail
            {
                FunctionGroup     = "MoveToNextSkill",
                TestCaseID        = "MoveToNextSkill_05",
                Description       = "On valid move, SaveChangesAsync is called exactly once",
                ExpectedResult    = "SaveChangesAsync called Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid move scenario", "Verifies DB commit" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // MoveToNextSkill_06 | E | Repository throws exception → propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB connection failed"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(new MoveToNextSkillCommand { UserExamId = "UE-001" }, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB connection failed");

            // Excel Log
            QACollector.LogTestCase("UserExam - Move To Next Skill", new TestCaseDetail
            {
                FunctionGroup     = "MoveToNextSkill",
                TestCaseID        = "MoveToNextSkill_06",
                Description       = "Repository throws exception → exception propagates",
                ExpectedResult    = "Exception thrown with message 'DB connection failed'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws Exception" }
            });
        }
    }
}
