using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Application.UseCases.MiniGame.Queries.Wordle;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class GetDailyWordleStatusHandlerTests
    {
        private static GetDailyWordleStatusHandler CreateHandler(
            Mock<IMiniGameRepository>? repo = null)
        {
            return new GetDailyWordleStatusHandler(
                (repo ?? new Mock<IMiniGameRepository>()).Object);
        }

        private static DailyWordle BuildDailyWordle(string id, string word, WordleLevel level = WordleLevel.Easy) => new()
        {
            DailyWordleId = id,
            Word          = word,
            Level         = level,
            GameDate      = DateOnly.FromDateTime(DateTime.Now)
        };

        private static GetDailyWordleStatusQuery BuildQuery(string userId = "USER-001") =>
            new() { UserId = userId };

        [Fact]
        public async Task Handle_NoDailyGamesForToday_ShouldReturnEmptyDashboard()
        {
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordlesByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<DailyWordle>());

            var result = await CreateHandler(mockRepo).Handle(BuildQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Levels.Should().BeEmpty();

            QACollector.LogTestCase("MiniGame - Daily Wordle Status", new TestCaseDetail
            {
                FunctionGroup     = "GetDailyWordleStatus",
                TestCaseID        = "TC-WDL-DST-01",
                Description       = "No daily games posted for today → empty Levels list in dashboard",
                ExpectedResult    = "Return 200, Levels = empty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!dailyGames.Any() => return empty dashboard" }
            });
        }

        [Fact]
        public async Task Handle_GamesToday_NoProgress_ShouldReturnDefaultStatus()
        {
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordlesByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<DailyWordle> { BuildDailyWordle("DW-001", "안녕해") });
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress>());

            var result = await CreateHandler(mockRepo).Handle(BuildQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Levels.Should().HaveCount(1);
            result.Data.Levels[0].IsWon.Should().BeFalse();
            result.Data.Levels[0].AttemptCount.Should().Be(0);
            result.Data.Levels[0].WordLength.Should().Be(3);

            QACollector.LogTestCase("MiniGame - Daily Wordle Status", new TestCaseDetail
            {
                FunctionGroup     = "GetDailyWordleStatus",
                TestCaseID        = "TC-WDL-DST-02",
                Description       = "1 game exists, user has no progress → IsWon=false, AttemptCount=0",
                ExpectedResult    = "Return 200, Levels[0].IsWon=false, AttemptCount=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userProgress == null => IsWon=false, AttemptCount=0" }
            });
        }

        [Fact]
        public async Task Handle_UserWon_ShouldSetIsWonTrue()
        {
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordlesByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<DailyWordle> { BuildDailyWordle("DW-001", "가나다") });

            var progress = new UserWordleProgress
            {
                DailyWordleId = "DW-001",
                IsWon         = true,
                AttemptCount  = 3,
                Guesses       = new List<string> { "라마바", "사아자", "가나다" }
            };
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress> { progress });

            var result = await CreateHandler(mockRepo).Handle(BuildQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Levels[0].IsWon.Should().BeTrue();
            result.Data.Levels[0].AttemptCount.Should().Be(3);
            result.Data.Levels[0].Attempts.Should().HaveCount(3);

            QACollector.LogTestCase("MiniGame - Daily Wordle Status", new TestCaseDetail
            {
                FunctionGroup     = "GetDailyWordleStatus",
                TestCaseID        = "TC-WDL-DST-03",
                Description       = "User already won with 3 guesses → IsWon=true, Attempts list populated",
                ExpectedResult    = "IsWon=true, AttemptCount=3, Attempts.Count=3",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userProgress.IsWon = true => IsWon = true" }
            });
        }

        [Fact]
        public async Task Handle_MultipleGames_ShouldReturnMultipleLevels()
        {
            var games = new List<DailyWordle>
            {
                BuildDailyWordle("DW-001", "가나다", WordleLevel.Easy),
                BuildDailyWordle("DW-002", "라마바사", WordleLevel.Medium),
                BuildDailyWordle("DW-003", "아자차카타파", WordleLevel.Hard)
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordlesByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(games);
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress>());

            var result = await CreateHandler(mockRepo).Handle(BuildQuery(), CancellationToken.None);

            result.Data!.Levels.Should().HaveCount(3);
            result.Data.Levels[0].Level.Should().Be(WordleLevel.Easy);
            result.Data.Levels[1].Level.Should().Be(WordleLevel.Medium);
            result.Data.Levels[2].MaxAttempts.Should().Be(6);

            QACollector.LogTestCase("MiniGame - Daily Wordle Status", new TestCaseDetail
            {
                FunctionGroup     = "GetDailyWordleStatus",
                TestCaseID        = "TC-WDL-DST-04",
                Description       = "3 games today → dashboard has 3 levels, MaxAttempts always 6",
                ExpectedResult    = "Levels.Count=3, MaxAttempts=6 each",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "dailyGames.Count == 3" }
            });
        }

        [Fact]
        public async Task Handle_Always_ShouldReturnTodayDate()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordlesByDateAsync(today, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<DailyWordle>());

            var result = await CreateHandler(mockRepo).Handle(BuildQuery(), CancellationToken.None);

            result.Data!.Date.Should().Be(today);

            QACollector.LogTestCase("MiniGame - Daily Wordle Status", new TestCaseDetail
            {
                FunctionGroup     = "GetDailyWordleStatus",
                TestCaseID        = "TC-WDL-DST-05",
                Description       = "Dashboard.Date always set to today's DateOnly",
                ExpectedResult    = "Dashboard.Date = DateOnly.FromDateTime(DateTime.Now)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "dashboard.Date = today" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordlesByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB timeout"));

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(BuildQuery(), CancellationToken.None));

            QACollector.LogTestCase("MiniGame - Daily Wordle Status", new TestCaseDetail
            {
                FunctionGroup     = "GetDailyWordleStatus",
                TestCaseID        = "TC-WDL-DST-06",
                Description       = "Repository throws exception → propagates unhandled",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetDailyWordlesByDateAsync throws" }
            });
        }
    }
}
