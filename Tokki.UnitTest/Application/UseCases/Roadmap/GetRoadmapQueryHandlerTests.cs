using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class GetRoadmapQueryHandlerTests
    {
        private static GetRoadmapQueryHandler CreateHandler(Mock<IUserRoadmapRepository>? repo = null)
            => new GetRoadmapQueryHandler((repo ?? MockUserRoadmapRepository.GetMock()).Object);

        // GetRoadmap_01 | A | No active roadmap → 404
        [Fact]
        public async Task Handle_NoActiveRoadmap_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            var result = await CreateHandler(repo).Handle(new GetRoadmapQuery("USER-001"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Get", new TestCaseDetail { FunctionGroup = "GetRoadmap", TestCaseID = "GetRoadmap_01", Description = "No active roadmap → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetActiveRoadmapByUserIdAsync returns null" } });
        }

        // GetRoadmap_02 | N | Active roadmap → RoadmapViewModel returned with correct fields
        [Fact]
        public async Task Handle_ActiveRoadmap_ShouldReturnViewModelWithCorrectId()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap("USER-001", "RM-001");
            var repo    = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            var result  = await CreateHandler(repo).Handle(new GetRoadmapQuery("USER-001"), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UserRoadmapId.Should().Be("RM-001");
            result.Data.Assessment.Should().Be("Good start");
            QACollector.LogTestCase("Roadmap - Get", new TestCaseDetail { FunctionGroup = "GetRoadmap", TestCaseID = "GetRoadmap_02", Description = "Active roadmap → ViewModel returned with UserRoadmapId and Assessment", ExpectedResult = "IsSuccess=true, UserRoadmapId='RM-001'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Active roadmap found", "ViewModel fields mapped" } });
        }

        // GetRoadmap_03 | N | Roadmap with weeks → weeks ordered and mapped
        [Fact]
        public async Task Handle_RoadmapWithWeeks_ShouldReturnOrderedWeeks()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            roadmap.Weeks.Add(new RoadmapWeek { RoadmapWeekId = "W2", WeekIndex = 2, Status = RoadmapWeekStatus.Locked, WeekFocusGoal = "Week 2", DailyTasks = new List<RoadmapDailyTask>() });
            roadmap.Weeks.Add(new RoadmapWeek { RoadmapWeekId = "W1", WeekIndex = 1, Status = RoadmapWeekStatus.InProgress, WeekFocusGoal = "Week 1", DailyTasks = new List<RoadmapDailyTask>() });
            var repo   = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            var result = await CreateHandler(repo).Handle(new GetRoadmapQuery("USER-001"), CancellationToken.None);
            result.Data!.Weeks.Should().HaveCount(2);
            result.Data.Weeks[0].WeekIndex.Should().Be(1);
            result.Data.Weeks[1].WeekIndex.Should().Be(2);
            QACollector.LogTestCase("Roadmap - Get", new TestCaseDetail { FunctionGroup = "GetRoadmap", TestCaseID = "GetRoadmap_03", Description = "Weeks ordered by WeekIndex ascending", ExpectedResult = "Weeks[0].WeekIndex=1, Weeks[1].WeekIndex=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "2 weeks out of order", "sorted by WeekIndex" } });
        }

        // GetRoadmap_04 | N | Week ProgressPercent calculated correctly
        [Fact]
        public async Task Handle_WeekWithTasks_ShouldCalculateProgressPercent()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            var week = new RoadmapWeek
            {
                RoadmapWeekId = "W1", WeekIndex = 1, Status = RoadmapWeekStatus.InProgress,
                DailyTasks = new List<RoadmapDailyTask>
                {
                    new RoadmapDailyTask { TaskId = "T1", IsCompleted = true,  Title = "T1", DayIndex = 1, TaskType = RoadmapTaskType.LearnTheory },
                    new RoadmapDailyTask { TaskId = "T2", IsCompleted = true,  Title = "T2", DayIndex = 1, TaskType = RoadmapTaskType.LearnTheory },
                    new RoadmapDailyTask { TaskId = "T3", IsCompleted = false, Title = "T3", DayIndex = 2, TaskType = RoadmapTaskType.LearnTheory },
                    new RoadmapDailyTask { TaskId = "T4", IsCompleted = false, Title = "T4", DayIndex = 2, TaskType = RoadmapTaskType.LearnTheory }
                }
            };
            roadmap.Weeks.Add(week);
            var repo   = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            var result = await CreateHandler(repo).Handle(new GetRoadmapQuery("USER-001"), CancellationToken.None);
            result.Data!.Weeks[0].ProgressPercent.Should().Be(50);
            QACollector.LogTestCase("Roadmap - Get", new TestCaseDetail { FunctionGroup = "GetRoadmap", TestCaseID = "GetRoadmap_04", Description = "2/4 completed tasks → ProgressPercent=50", ExpectedResult = "ProgressPercent=50", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "2 completed, 2 incomplete tasks", "50%" } });
        }

        // GetRoadmap_05 | B | Empty week → ProgressPercent=0
        [Fact]
        public async Task Handle_EmptyWeek_ShouldHaveProgressPercentZero()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            roadmap.Weeks.Add(new RoadmapWeek { RoadmapWeekId = "W1", WeekIndex = 1, Status = RoadmapWeekStatus.Locked, DailyTasks = new List<RoadmapDailyTask>() });
            var repo   = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            var result = await CreateHandler(repo).Handle(new GetRoadmapQuery("USER-001"), CancellationToken.None);
            result.Data!.Weeks[0].ProgressPercent.Should().Be(0);
            QACollector.LogTestCase("Roadmap - Get", new TestCaseDetail { FunctionGroup = "GetRoadmap", TestCaseID = "GetRoadmap_05", Description = "Empty week (no tasks) → ProgressPercent=0", ExpectedResult = "ProgressPercent=0", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No tasks in week", "guard: 0 tasks → 0%" } });
        }

        // GetRoadmap_06 | B | GetActiveRoadmapByUserIdAsync called with correct UserId
        [Fact]
        public async Task Handle_ValidQuery_GetActiveRoadmapCalledWithCorrectUserId()
        {
            var repo = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            await CreateHandler(repo).Handle(new GetRoadmapQuery("USER-SPECIFIC"), CancellationToken.None);
            repo.Verify(x => x.GetActiveRoadmapByUserIdAsync("USER-SPECIFIC", It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Get", new TestCaseDetail { FunctionGroup = "GetRoadmap", TestCaseID = "GetRoadmap_06", Description = "GetActiveRoadmapByUserIdAsync called with correct UserId", ExpectedResult = "Verify Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserId passed correctly" } });
        }
    }
}
