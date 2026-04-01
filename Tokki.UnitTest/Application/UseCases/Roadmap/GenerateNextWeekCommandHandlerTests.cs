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

            return new GenerateNextWeekCommandHandler(
                (repo ?? MockUserRoadmapRepository.GetMock()).Object,
                mockAi.Object,
                mockExam.Object,
                mockProf.Object,
                GetIdGen().Object,
                mockLog.Object);
        }

        // TC-RM-GNW-01 | A | Week not found → 404
        [Fact]
        public async Task Handle_WeekNotFound_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(week: null);
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "WEEK-MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "TC-RM-GNW-01", Description = "Week not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetWeekByIdAsync returns null" } });
        }

        // TC-RM-GNW-02 | A | Week has exam but user hasn't submitted → 400
        [Fact]
        public async Task Handle_WeekHasExamButNotSubmitted_ShouldReturn400()
        {
            var week   = MockUserRoadmapRepository.GetSampleWeek(examId: "EXAM-001");
            var repo   = MockUserRoadmapRepository.GetMock(week: week, userExam: null); // null = not submitted
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = week.RoadmapWeekId }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "TC-RM-GNW-02", Description = "Week has exam but not submitted → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "WeeklyExamId set, user exam = null" } });
        }

        // TC-RM-GNW-03 | A | User does not own the week's roadmap → 403
        [Fact]
        public async Task Handle_WrongUser_ShouldReturn403()
        {
            var week = MockUserRoadmapRepository.GetSampleWeek(userId: "OWNER-001");
            var repo = MockUserRoadmapRepository.GetMock(week: week);
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "OTHER-USER", FinishedWeekId = week.RoadmapWeekId }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "TC-RM-GNW-03", Description = "Wrong user for roadmap → 403", ExpectedResult = "IsSuccess=false, 403", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserId mismatch with roadmap owner" } });
        }

        // TC-RM-GNW-04 | N | No next week exists → week completed, 200 (all done)
        [Fact]
        public async Task Handle_NoNextWeek_ShouldReturnRoadmapComplete200()
        {
            var week = MockUserRoadmapRepository.GetSampleWeek("W1", "USER-001", "RM-001", 1, null);
            var repo = MockUserRoadmapRepository.GetMock(week: week, weekByIndex: null); // no next week
            var result = await CreateHandler(repo).Handle(new GenerateNextWeekCommand { UserId = "USER-001", FinishedWeekId = "W1" }, CancellationToken.None);
            // Handler returns Failure("Bạn đã hoàn thành!", 200) — which is IsSuccess=false with status 200
            result.StatusCode.Should().Be(200);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "TC-RM-GNW-04", Description = "No next week → 'Bạn đã hoàn thành!' returned with 200", ExpectedResult = "StatusCode=200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetWeekByIndexAsync returns null (last week)", "all done message" } });
        }

        // TC-RM-GNW-05 | A | Next week exists but AI returns null → 500
        [Fact]
        public async Task Handle_AiReturnsNull_ShouldReturn500()
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
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "TC-RM-GNW-05", Description = "AI returns null plan → 500", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GenerateNextWeekPlanAsync returns null" } });
        }

        // TC-RM-GNW-06 | N | Happy path: next week generated with tasks → success
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
            result.Data!.IsGenerated.Should().BeTrue();
            QACollector.LogTestCase("Roadmap - Generate Next Week", new TestCaseDetail { FunctionGroup = "GenerateNextWeek", TestCaseID = "TC-RM-GNW-06", Description = "Happy path: AI generates plan → success, IsGenerated=true", ExpectedResult = "IsSuccess=true, Data.IsGenerated=true", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "AI returns valid plan", "next week generated" } });
        }
    }
}
