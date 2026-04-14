using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Leaderboard.DTOs;
using Tokki.Application.UseCases.Leaderboard.Queries;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Leaderboard
{
    public class GetLeaderboardQueryHandlerTests
    {
        private static GetLeaderboardQueryHandler CreateHandler(
            Mock<IAccountRepository>? accountRepo = null)
        {
            return new GetLeaderboardQueryHandler(
                (accountRepo ?? new Mock<IAccountRepository>()).Object);
        }

        private static List<LeaderboardItemDto> BuildItems(int count) =>
            System.Linq.Enumerable.Range(1, count)
                .Select(i => new LeaderboardItemDto
                {
                    UserId   = $"USER-{i:000}",
                    FullName = $"Player {i}",
                    TotalXP  = 1000 - (i * 10)
                })
                .ToList();

        // TC-01: Empty result → return empty list 200
        [Fact]
        public async Task Handle_NoPlayers_ShouldReturnEmptyList()
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(x => x.GetLeaderboardAsync(It.IsAny<LeaderboardTimeFrame>(), It.IsAny<int>()))
                .ReturnsAsync(new List<LeaderboardItemDto>());

            var result = await CreateHandler(repo)
                .Handle(new GetLeaderboardQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Leaderboard - Get", new TestCaseDetail
            {
                FunctionGroup = "GetLeaderboard", TestCaseID = "TC-LDB-01",
                Description = "No players registered → Return 200 empty list",
                ExpectedResult = "Return 200, Data=[]", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetLeaderboardAsync returns empty" }
            });
        }

        // TC-02: Top 10 AllTime
        [Fact]
        public async Task Handle_AllTimeTop10_ShouldReturn10Players()
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(x => x.GetLeaderboardAsync(LeaderboardTimeFrame.AllTime, 10))
                .ReturnsAsync(BuildItems(10));

            var query  = new GetLeaderboardQuery { TimeFrame = LeaderboardTimeFrame.AllTime, Top = 10 };
            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(10);

            QACollector.LogTestCase("Leaderboard - Get", new TestCaseDetail
            {
                FunctionGroup = "GetLeaderboard", TestCaseID = "TC-LDB-02",
                Description = "AllTime Top=10 → Return 200 with 10 items",
                ExpectedResult = "Return 200, Data.Count=10", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TimeFrame=AllTime, Top=10" }
            });
        }

        // TC-03: Weekly TimeFrame
        [Fact]
        public async Task Handle_WeeklyTimeFrame_ShouldPassCorrectTimeFrame()
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(x => x.GetLeaderboardAsync(LeaderboardTimeFrame.Week, 20))
                .ReturnsAsync(BuildItems(5));

            var query  = new GetLeaderboardQuery { TimeFrame = LeaderboardTimeFrame.Week, Top = 20 };
            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.GetLeaderboardAsync(LeaderboardTimeFrame.Week, 20), Times.Once);

            QACollector.LogTestCase("Leaderboard - Get", new TestCaseDetail
            {
                FunctionGroup = "GetLeaderboard", TestCaseID = "TC-LDB-03",
                Description = "Weekly TimeFrame → GetLeaderboardAsync called with Weekly",
                ExpectedResult = "Return 200, Weekly filter applied", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TimeFrame=Weekly passed to repo" }
            });
        }

        // TC-04: Monthly TimeFrame
        [Fact]
        public async Task Handle_MonthlyTimeFrame_ShouldPassCorrectArgs()
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(x => x.GetLeaderboardAsync(LeaderboardTimeFrame.Month, 5))
                .ReturnsAsync(BuildItems(3));

            var query  = new GetLeaderboardQuery { TimeFrame = LeaderboardTimeFrame.Month, Top = 5 };
            var result = await CreateHandler(repo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            repo.Verify(x => x.GetLeaderboardAsync(LeaderboardTimeFrame.Month, 5), Times.Once);

            QACollector.LogTestCase("Leaderboard - Get", new TestCaseDetail
            {
                FunctionGroup = "GetLeaderboard", TestCaseID = "TC-LDB-04",
                Description = "Monthly Top=5 → Repo called correctly, Return 200",
                ExpectedResult = "Return 200, GetLeaderboardAsync(Monthly,5) called", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TimeFrame=Monthly, Top=5 passed to repo" }
            });
        }

        // TC-05: Default values used (AllTime, Top=20)
        [Fact]
        public async Task Handle_DefaultQuery_ShouldUseAllTimeAndTop20()
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(x => x.GetLeaderboardAsync(LeaderboardTimeFrame.AllTime, 20))
                .ReturnsAsync(BuildItems(20));

            var result = await CreateHandler(repo)
                .Handle(new GetLeaderboardQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(20);
            repo.Verify(x => x.GetLeaderboardAsync(LeaderboardTimeFrame.AllTime, 20), Times.Once);

            QACollector.LogTestCase("Leaderboard - Get", new TestCaseDetail
            {
                FunctionGroup = "GetLeaderboard", TestCaseID = "TC-LDB-05",
                Description = "Default query (AllTime, Top=20) → 200, 20 items",
                ExpectedResult = "Return 200, Data.Count=20", StatusRound1 = "Passed",
                TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Default: AllTime, Top=20" }
            });
        }

        // TC-06: Repository throws → propagates
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(x => x.GetLeaderboardAsync(It.IsAny<LeaderboardTimeFrame>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("DB timeout"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(repo).Handle(new GetLeaderboardQuery(), CancellationToken.None));

            QACollector.LogTestCase("Leaderboard - Get", new TestCaseDetail
            {
                FunctionGroup = "GetLeaderboard", TestCaseID = "TC-LDB-06",
                Description = "Repository throws → exception propagates",
                ExpectedResult = "Throws Exception", StatusRound1 = "Passed",
                TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetLeaderboardAsync throws" }
            });
        }
    }
}
