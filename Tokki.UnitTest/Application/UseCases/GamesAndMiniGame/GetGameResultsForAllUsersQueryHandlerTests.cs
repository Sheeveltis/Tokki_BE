using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Games.Queries.GetGameResultsForAllUsers;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class GetGameResultsForAllUsersQueryHandlerTests
    {
        private static GetGameResultsForAllUsersQueryHandler CreateHandler(
            Mock<IGameMatchSessionRepository>? sessionRepo = null,
            Mock<IAccountRepository>? accountRepo = null)
        {
            return new GetGameResultsForAllUsersQueryHandler(
                (sessionRepo ?? new Mock<IGameMatchSessionRepository>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object);
        }

        private static GetGameResultsForAllUsersQuery DefaultQuery => new()
        {
            GameId         = "GAME-001",
            TopicId        = "TOPIC-001",
            gameDifficulty = GameDifficulty.Easy,
            PageNumber     = 1,
            PageSize       = 10
        };

        private static GameMatchSession BuildSession(string userId, int best = 100, int latest = 80) => new()
        {
            GameMatchSessionId = $"SES-{userId}",
            UserId             = userId,
            GameId             = "GAME-001",
            TopicId            = "TOPIC-001",
            BestScore          = best,
            LatestScore        = latest,
            GameDifficulty     = GameDifficulty.Easy,
            CreatedAt          = DateTime.UtcNow
        };

        [Fact]
        public async Task Handle_NoSessions_ShouldReturnEmptyPagedResult()
        {
            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetPagedByGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
                       .ReturnsAsync((new List<GameMatchSession>().AsReadOnly(), 0));

            var result = await CreateHandler(sessionRepo: mockSession).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Games - Get Results All Users", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultsForAllUsers",
                TestCaseID        = "TC-GAME-GRA-01",
                Description       = "No sessions in DB → empty PagedResult",
                ExpectedResult    = "Return 200, Items = empty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "sessions.Count == 0" }
            });
        }

        [Fact]
        public async Task Handle_MultipleSessions_ShouldMapAccountInfo()
        {
            var sessions = new List<GameMatchSession>
            {
                BuildSession("USER-001", 300, 200),
                BuildSession("USER-002", 150, 100)
            }.AsReadOnly();

            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetPagedByGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
                       .ReturnsAsync((sessions, 2));

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync("USER-001"))
                       .ReturnsAsync(new AccountBasicInfoDTO { FullName = "Player One", AvatarUrl = "p1.png" });
            mockAccount.Setup(x => x.GetBasicInfoAsync("USER-002"))
                       .ReturnsAsync(new AccountBasicInfoDTO { FullName = "Player Two", AvatarUrl = "p2.png" });

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items[0].UserName.Should().Be("Player One");
            result.Data.Items[0].BestScore.Should().Be(300);
            result.Data.Items[1].UserName.Should().Be("Player Two");

            QACollector.LogTestCase("Games - Get Results All Users", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultsForAllUsers",
                TestCaseID        = "TC-GAME-GRA-02",
                Description       = "2 sessions found; account info fetched and mapped",
                ExpectedResult    = "Return 200, Items=2, correct UserNames and BestScores",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "sessions.Count == 2", "account info resolved" }
            });
        }

        [Fact]
        public async Task Handle_SameUserDifferentTopics_ShouldCacheAccountInfo()
        {
            var sessions = new List<GameMatchSession>
            {
                BuildSession("USER-001", 100, 80),
                new() { GameMatchSessionId = "S2", UserId = "USER-001", GameId = "GAME-001", TopicId = "TOPIC-002", BestScore = 200, LatestScore = 190, GameDifficulty = GameDifficulty.Easy, CreatedAt = DateTime.UtcNow }
            }.AsReadOnly();

            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetPagedByGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
                       .ReturnsAsync((sessions, 2));

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync("USER-001"))
                       .ReturnsAsync(new AccountBasicInfoDTO { FullName = "Cached User" });

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(DefaultQuery, CancellationToken.None);

            mockAccount.Verify(x => x.GetBasicInfoAsync("USER-001"), Times.Once);
            result.Data!.Items.Should().HaveCount(2);

            QACollector.LogTestCase("Games - Get Results All Users", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultsForAllUsers",
                TestCaseID        = "TC-GAME-GRA-03",
                Description       = "Same user in 2 sessions → GetBasicInfoAsync called only once (caching)",
                ExpectedResult    = "GetBasicInfoAsync called Once, Items = 2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userInfoCache.ContainsKey(userId) => skip fetch" }
            });
        }

        [Fact]
        public async Task Handle_AccountNotFound_ShouldFallbackEmptyStrings()
        {
            var sessions = new List<GameMatchSession> { BuildSession("GHOST-USER") }.AsReadOnly();

            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetPagedByGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
                       .ReturnsAsync((sessions, 1));

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync("GHOST-USER")).ReturnsAsync((AccountBasicInfoDTO?)null);

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items[0].UserName.Should().Be(string.Empty);

            QACollector.LogTestCase("Games - Get Results All Users", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultsForAllUsers",
                TestCaseID        = "TC-GAME-GRA-04",
                Description       = "Account not found for userId → fallback to string.Empty",
                ExpectedResult    = "Return 200, UserName = ''",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "info?.FullName ?? string.Empty" }
            });
        }

        [Fact]
        public async Task Handle_PaginationQuery_ShouldReturnCorrectMetadata()
        {
            var fiveSessions = new List<GameMatchSession> {
                BuildSession("U1"), BuildSession("U2"), BuildSession("U3"), BuildSession("U4"), BuildSession("U5")
            }.AsReadOnly();

            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetPagedByGameTopicAsync(
                    "GAME-001", "TOPIC-001", GameDifficulty.Easy, 2, 5))
                       .ReturnsAsync((fiveSessions, 20));

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>())).ReturnsAsync(new AccountBasicInfoDTO { FullName = "Test" });

            var query = new GetGameResultsForAllUsersQuery
            {
                GameId = "GAME-001",
                TopicId = "TOPIC-001",
                gameDifficulty = GameDifficulty.Easy,
                PageNumber = 2,
                PageSize = 5
            };

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(query, CancellationToken.None);

            result.Data!.TotalCount.Should().Be(20);
            result.Data.PageNumber.Should().Be(2);
            result.Data.TotalPages.Should().Be(4);

            QACollector.LogTestCase("Games - Get Results All Users", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultsForAllUsers",
                TestCaseID        = "TC-GAME-GRA-05",
                Description       = "Page 2/5, 20 total → TotalPages=4",
                ExpectedResult    = "Return 200, TotalPages=4",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PagedResult.Create(items, 20, 2, 5)" }
            });
        }

        [Fact]
        public async Task Handle_ValidSessions_ShouldMapScoresCorrectly()
        {
            var session = BuildSession("USER-001", best: 999, latest: 777);
            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetPagedByGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
                       .ReturnsAsync((new List<GameMatchSession> { session }.AsReadOnly(), 1));

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>())).ReturnsAsync(new AccountBasicInfoDTO { FullName = "Top Player" });

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(DefaultQuery, CancellationToken.None);

            result.Data!.Items[0].BestScore.Should().Be(999);
            result.Data.Items[0].LatestScore.Should().Be(777);

            QACollector.LogTestCase("Games - Get Results All Users", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultsForAllUsers",
                TestCaseID        = "TC-GAME-GRA-06",
                Description       = "BestScore=999 and LatestScore=777 correctly mapped to DTO",
                ExpectedResult    = "Return 200, BestScore=999, LatestScore=777",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "dto.BestScore = s.BestScore", "dto.LatestScore = s.LatestScore" }
            });
        }
    }
}
