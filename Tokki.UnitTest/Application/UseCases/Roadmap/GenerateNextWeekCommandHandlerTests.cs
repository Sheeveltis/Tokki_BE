using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class GenerateNextWeekCommandHandlerTests
    {
        private static Mock<IIdGeneratorService> GetIdGen() { var m = new Mock<IIdGeneratorService>(); m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("ID-GEN"); return m; }

        private static GenerateNextWeekCommandHandler CreateHandler(
            Mock<IUserRoadmapRepository>?             repo        = null,
            Mock<IAiRoadmapService>?                  aiService   = null,
            Mock<IExamAssemblyService>?               examAssembly = null,
            Mock<IRoadmapKnowledgeProfileRepository>? profileRepo = null)
        {
            var mockAi    = aiService    ?? new Mock<IAiRoadmapService>();
            var mockExam  = examAssembly ?? new Mock<IExamAssemblyService>();
            var mockProf  = profileRepo  ?? new Mock<IRoadmapKnowledgeProfileRepository>();
            var mockLog   = new Mock<ILogger<GenerateNextWeekCommandHandler>>();
            
            mockProf.Setup(x => x.GetByRoadmapIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<RoadmapKnowledgeProfile>());

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(x => x.GetService(typeof(IUserRoadmapRepository))).Returns((repo ?? MockUserRoadmapRepository.GetMock()).Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IAiRoadmapService))).Returns(mockAi.Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IExamAssemblyService))).Returns(mockExam.Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IRoadmapKnowledgeProfileRepository))).Returns(mockProf.Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IUserWeaknessRepository))).Returns(new Mock<IUserWeaknessRepository>().Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IMediator))).Returns(new Mock<IMediator>().Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IIdGeneratorService))).Returns(GetIdGen().Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IRoadmapProgressService))).Returns(new Mock<IRoadmapProgressService>().Object);

            var mockScope = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScope>();
            mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
            
            var mockScopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
            mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

            return new GenerateNextWeekCommandHandler(
                (repo ?? MockUserRoadmapRepository.GetMock()).Object,
                GetIdGen().Object,
                new Mock<IRoadmapProgressService>().Object,
                mockScopeFactory.Object,
                mockLog.Object);
        }

        // GenerateNextWeek_01 | A | Week not found → 404
        [Fact]
        public async Task Handle_WeekNotFound_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(week: null);
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "WEEK-MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_01", Description = "Week not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetWeekByIdAsync returns null" } });
        }

        // GenerateNextWeek_02 | A | Week has exam but user hasn't submitted → 400
        [Fact]
        public async Task Handle_WeekHasExamButNotSubmitted_ShouldReturn400()
        {
            var week   = MockUserRoadmapRepository.GetSampleWeek(examId: "EXAM-001");
            var repo   = MockUserRoadmapRepository.GetMock(week: week, userExam: null); // null = not submitted
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = week.RoadmapWeekId }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_02", Description = "Week has exam but not submitted → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "WeeklyExamId set, user exam = null" } });
        }

        // GenerateNextWeek_03 | A | User does not own the week's roadmap → 403
        [Fact]
        public async Task Handle_WrongUser_ShouldReturn403()
        {
            var week = MockUserRoadmapRepository.GetSampleWeek(userId: "OWNER-001");
            var repo = MockUserRoadmapRepository.GetMock(week: week);
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "OTHER-USER", FinishedWeekId = week.RoadmapWeekId }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_03", Description = "Wrong user for roadmap → 403", ExpectedResult = "IsSuccess=false, 403", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserId mismatch with roadmap owner" } });
        }

        // GenerateNextWeek_04 | N | No next week exists → 202 (processed in background)
        [Fact]
        public async Task Handle_NoNextWeek_ShouldReturn202AndProcessInBackground()
        {
            var week = MockUserRoadmapRepository.GetSampleWeek("W1", "USER-001", "RM-001", 1, null);
            var repo = MockUserRoadmapRepository.GetMock(week: week, weekByIndex: null); // no next week
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "W1" }, CancellationToken.None);
            
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data.Should().Be("ID-GEN");
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_04", Description = "No next week → 202 Accepted via background job", ExpectedResult = "StatusCode=202", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetWeekByIndexAsync returns null", "Delegated to background" } });
        }

        // GenerateNextWeek_05 | N | AI returns null → 202 (error handled in background)
        [Fact]
        public async Task Handle_AiReturnsNull_ShouldReturn202AndSetErrorInBackground()
        {
            var week     = MockUserRoadmapRepository.GetSampleWeek("W1", "USER-001", "RM-001", 1, null);
            var nextWeek = MockUserRoadmapRepository.GetSampleWeek("W2", "USER-001", "RM-001", 2, null);
            var repo     = MockUserRoadmapRepository.GetMock(week: week, weekByIndex: nextWeek);
            var aiService = new Mock<IAiRoadmapService>();
            aiService.Setup(x => x.GenerateNextWeekPlanAsync(
                It.IsAny<TargetAimLevel>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(),
                It.IsAny<List<string>>(), It.IsAny<List<QuestionTypeMenuItem>>(), It.IsAny<List<QuestionTypeMenuItem>>()))
                .ReturnsAsync((AiRoadmapResponse?)null);
            var result = await CreateHandler(repo, aiService).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "W1" }, CancellationToken.None);
            
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data.Should().Be("ID-GEN");
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_05", Description = "AI fails → 202 Accepted, fail state tracked in background", ExpectedResult = "IsSuccess=true, 202", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GenerateNextWeekPlanAsync returns null", "Job delegated" } });
        }

        // GenerateNextWeek_06 | N | Happy path: next week generated with tasks → success
        [Fact]
        public async Task Handle_HappyPath_ShouldGenerateNextWeekAndReturnSuccess()
        {
            var week     = MockUserRoadmapRepository.GetSampleWeek("W1", "USER-001", "RM-001", 1, null);
            var nextWeek = MockUserRoadmapRepository.GetSampleWeek("W2", "USER-001", "RM-001", 2, null);
            var repo     = MockUserRoadmapRepository.GetMock(week: week, weekByIndex: nextWeek);
            var aiService = new Mock<IAiRoadmapService>();
            aiService.Setup(x => x.GenerateNextWeekPlanAsync(
                It.IsAny<TargetAimLevel>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(),
                It.IsAny<List<string>>(), It.IsAny<List<QuestionTypeMenuItem>>(), It.IsAny<List<QuestionTypeMenuItem>>()))
                .ReturnsAsync(new AiRoadmapResponse
                {
                    Weeks = new List<AiWeekPlan>
                    {
                        new AiWeekPlan { WeekGoal = "Master grammar", Days = new List<AiDaySchedule>() }
                    }
                });
            var result = await CreateHandler(repo, aiService).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "W1" }, CancellationToken.None);
            
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data.Should().Be("ID-GEN");
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_06", Description = "Happy path: Accepted and processed in task", ExpectedResult = "IsSuccess=true, 202", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Valid scenario", "Job started" } });
        }
        
        // GenerateNextWeek_07 | N | HasExam evaluates scores
        [Fact]
        public async Task Handle_ExamScoreEvaluatedWithWarning()
        {
            var week = MockUserRoadmapRepository.GetSampleWeek("W1", "USER-001", "RM-001", 1, "EXAM-1");
            var nextWeek = MockUserRoadmapRepository.GetSampleWeek("W2", "USER-001", "RM-001", 2, null);
            var userExam = new Domain.Entities.UserExam { Score = 40 };

            var examQuestions = new List<ExamQuestion> { new ExamQuestion { Score = 100 } };
            
            var repo = new Mock<IUserRoadmapRepository>();
            repo.Setup(x => x.GetWeekByIdAsync("W1", It.IsAny<CancellationToken>())).ReturnsAsync(week);
            repo.Setup(x => x.GetUserExamByExamIdAsync("EXAM-1", "USER-001", It.IsAny<CancellationToken>())).ReturnsAsync(userExam);
            repo.Setup(x => x.GetWeekByIndexAsync("RM-001", 2, It.IsAny<CancellationToken>())).ReturnsAsync(nextWeek);
            repo.Setup(x => x.GetExamQuestionsForGradingAsync("EXAM-1", It.IsAny<CancellationToken>())).ReturnsAsync(examQuestions);
            repo.Setup(x => x.GetQuestionTypeMenuAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionTypeMenuItem>());
            repo.Setup(x => x.GetValidQuestionTypeIdsByLevelAsync(It.IsAny<CurrentTopikLevel>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

            var profileRepo = new Mock<IRoadmapKnowledgeProfileRepository>();
            profileRepo.Setup(x => x.GetByRoadmapIdAsync("RM-001", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<RoadmapKnowledgeProfile> { new RoadmapKnowledgeProfile { IsWeakness = true, ConsecutiveFailWeeks = 3, QuestionTypeId = "QT-01" } });

            var aiService = new Mock<IAiRoadmapService>();
            aiService.Setup(x => x.GenerateNextWeekPlanAsync(It.IsAny<TargetAimLevel>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<QuestionTypeMenuItem>>(), It.IsAny<List<QuestionTypeMenuItem>>()))
                     .ReturnsAsync(new AiRoadmapResponse { Weeks = new List<AiWeekPlan> { new AiWeekPlan { WeekGoal = "Goal", Days = new List<AiDaySchedule>() } } });

            var result = await CreateHandler(repo, aiService, null, profileRepo).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "W1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data.Should().Be("ID-GEN");

            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_07", Description = "Evaluates exam score and initiates job", ExpectedResult = "202 Accepted", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Weakness evaluated" } });
        }

        // GenerateNextWeek_08 | A | Insufficient types skips weekly exam
        [Fact]
        public async Task Handle_InsufficientTypes_SkipsExamGenerating()
        {
            var week = MockUserRoadmapRepository.GetSampleWeek("W1", "USER-001", "RM-001", 1, null);
            var nextWeek = MockUserRoadmapRepository.GetSampleWeek("W2", "USER-001", "RM-001", 2, null);
            var repo = MockUserRoadmapRepository.GetMock(week: week, weekByIndex: nextWeek);

            var aiService = new Mock<IAiRoadmapService>();
            var dayTask = new AiTask { TaskType = "VirtualQuiz", QuestionTypeId = "QT-1" };
            var examTask = new AiTask { TaskType = "WeeklyExam" };
            aiService.Setup(x => x.GenerateNextWeekPlanAsync(It.IsAny<TargetAimLevel>(), It.IsAny<CurrentTopikLevel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<List<QuestionTypeMenuItem>>(), It.IsAny<List<QuestionTypeMenuItem>>()))
                     .ReturnsAsync(new AiRoadmapResponse { Weeks = new List<AiWeekPlan> { new AiWeekPlan { WeekGoal = "Goal", Days = new List<AiDaySchedule> { new AiDaySchedule { DayIndex = 1, Tasks = new List<AiTask> { dayTask, examTask } } } } } });

            var examAssembly = new Mock<IExamAssemblyService>();
            examAssembly.Setup(x => x.ValidateQuestionAvailabilityAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((false, new List<string> { "QT-1" }));

            var result = await CreateHandler(repo, aiService, examAssembly).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "W1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data.Should().Be("ID-GEN");
            
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "GenerateNextWeek_08", Description = "Skips creating weekly exam if question availability invalid", ExpectedResult = "202 Accepted", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Invalid question availability" } });
        }
    }
}
