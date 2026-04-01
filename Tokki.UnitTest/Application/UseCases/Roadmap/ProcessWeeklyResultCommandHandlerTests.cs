using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.Commands.ProcessWeeklyResult;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class ProcessWeeklyResultCommandHandlerTests
    {
        private static Mock<IIdGeneratorService> GetIdGenMock() { var m = new Mock<IIdGeneratorService>(); m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("ID-GEN"); return m; }

        private static ProcessWeeklyResultCommandHandler CreateHandler(
            Mock<IUserRoadmapRepository>?             roadmapRepo = null,
            Mock<IUserWeaknessRepository>?            weakRepo    = null,
            Mock<IRoadmapKnowledgeProfileRepository>? profileRepo = null,
            Mock<IUserExamRepository>?                examRepo    = null)
        {
            var mockWeak    = weakRepo    ?? new Mock<IUserWeaknessRepository>();
            var mockProfile = profileRepo ?? new Mock<IRoadmapKnowledgeProfileRepository>();
            var mockExam    = examRepo    ?? new Mock<IUserExamRepository>();
            mockWeak.Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserWeakness>());
            mockWeak.Setup(x => x.AddAsync(It.IsAny<UserWeakness>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockWeak.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockProfile.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((RoadmapKnowledgeProfile?)null);
            mockProfile.Setup(x => x.AddAsync(It.IsAny<RoadmapKnowledgeProfile>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockProfile.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return new ProcessWeeklyResultCommandHandler(
                (roadmapRepo ?? MockUserRoadmapRepository.GetMock()).Object,
                mockWeak.Object,
                mockProfile.Object,
                mockExam.Object,
                GetIdGenMock().Object);
        }

        private static Tokki.Domain.Entities.UserExam GetCompletedExam(string userId = "USER-001", string examId = "EXAM-001") => new Tokki.Domain.Entities.UserExam
        {
            UserExamId = "UE-001", UserId = userId, ExamId = examId,
            Status     = UserExamStatus.Completed, Score = 80,
            UserExamAnswers = new List<UserExamAnswer>(),
            Exam            = new Tokki.Domain.Entities.Exam { ExamId = examId, ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart>() } }
        };

        // TC-RM-PWR-01 | A | UserExam not found → 404
        [Fact]
        public async Task Handle_UserExamNotFound_ShouldReturn404()
        {
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tokki.Domain.Entities.UserExam?)null);
            var result = await CreateHandler(examRepo: examRepo).Handle(new ProcessWeeklyResultCommand { UserId = "USER-001", UserExamId = "UE-MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Process Weekly Result", new TestCaseDetail { FunctionGroup = "ProcessWeeklyResult", TestCaseID = "TC-RM-PWR-01", Description = "UserExam not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-RM-PWR-02 | A | Exam belongs to different user → 403
        [Fact]
        public async Task Handle_WrongUser_ShouldReturn403()
        {
            var exam = GetCompletedExam("OWNER-001");
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            var result = await CreateHandler(examRepo: examRepo).Handle(new ProcessWeeklyResultCommand { UserId = "OTHER-USER", UserExamId = "UE-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            QACollector.LogTestCase("Roadmap - Process Weekly Result", new TestCaseDetail { FunctionGroup = "ProcessWeeklyResult", TestCaseID = "TC-RM-PWR-02", Description = "Wrong user → 403", ExpectedResult = "IsSuccess=false, 403", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "session.UserId != request.UserId" } });
        }

        // TC-RM-PWR-03 | A | Exam status not Completed → 400
        [Fact]
        public async Task Handle_ExamNotCompleted_ShouldReturn400()
        {
            var exam = GetCompletedExam("USER-001");
            exam.Status = UserExamStatus.InProgress;
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            var result = await CreateHandler(examRepo: examRepo).Handle(new ProcessWeeklyResultCommand { UserId = "USER-001", UserExamId = "UE-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Roadmap - Process Weekly Result", new TestCaseDetail { FunctionGroup = "ProcessWeeklyResult", TestCaseID = "TC-RM-PWR-03", Description = "Exam not Completed → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status=InProgress (not Completed)" } });
        }

        // TC-RM-PWR-04 | A | No active roadmap → 404
        [Fact]
        public async Task Handle_NoActiveRoadmap_ShouldReturn404()
        {
            var exam = GetCompletedExam("USER-001");
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            var roadmapRepo = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            var result = await CreateHandler(roadmapRepo: roadmapRepo, examRepo: examRepo).Handle(new ProcessWeeklyResultCommand { UserId = "USER-001", UserExamId = "UE-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Process Weekly Result", new TestCaseDetail { FunctionGroup = "ProcessWeeklyResult", TestCaseID = "TC-RM-PWR-04", Description = "No active roadmap → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetActiveRoadmapByUserIdAsync returns null" } });
        }

        // TC-RM-PWR-05 | A | Exam week not matching roadmap → 400
        [Fact]
        public async Task Handle_ExamWeekNotInRoadmap_ShouldReturn400()
        {
            var exam = GetCompletedExam("USER-001", "EXAM-NOT-IN-ROADMAP");
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            // Weeks has no matching WeeklyExamId
            var roadmapRepo = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            var result = await CreateHandler(roadmapRepo: roadmapRepo, examRepo: examRepo).Handle(new ProcessWeeklyResultCommand { UserId = "USER-001", UserExamId = "UE-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Roadmap - Process Weekly Result", new TestCaseDetail { FunctionGroup = "ProcessWeeklyResult", TestCaseID = "TC-RM-PWR-05", Description = "Exam not in current roadmap weeks → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No week with matching WeeklyExamId" } });
        }

        // TC-RM-PWR-06 | N | Exam matched → score calculated and success returned
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccessWithScorePercent()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap("USER-001", "RM-001");
            var examId  = "EXAM-WEEKLY-01";
            roadmap.Weeks.Add(new RoadmapWeek { RoadmapWeekId = "W1", WeekIndex = 1, WeeklyExamId = examId, DailyTasks = new List<RoadmapDailyTask>() });
            var exam = new Tokki.Domain.Entities.UserExam
            {
                UserExamId = "UE-001", UserId = "USER-001", ExamId = examId,
                Status = UserExamStatus.Completed, Score = 0,
                UserExamAnswers = new List<UserExamAnswer>(),
                Exam = new Tokki.Domain.Entities.Exam { ExamId = examId, ExamTemplate = new ExamTemplate { TemplateParts = new List<TemplatePart>() } }
            };
            var examRepo = new Mock<IUserExamRepository>();
            examRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(exam);
            var roadmapRepo = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            var result = await CreateHandler(roadmapRepo: roadmapRepo, examRepo: examRepo).Handle(new ProcessWeeklyResultCommand { UserId = "USER-001", UserExamId = "UE-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ScorePercent.Should().Be(0); // score=0, maxScore=0 → 0%
            QACollector.LogTestCase("Roadmap - Process Weekly Result", new TestCaseDetail { FunctionGroup = "ProcessWeeklyResult", TestCaseID = "TC-RM-PWR-06", Description = "Valid request, exam in roadmap → success with ScorePercent=0 (no template parts)", ExpectedResult = "IsSuccess=true, ScorePercent=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exam matched in roadmap", "no template parts → 0%" } });
        }
    }
}
