using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Solitaire.Queries.GetSolitaireResultForUser;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Solitaire
{
    public class GetSolitaireResultForUserQueryHandlerTests
    {
        private static Mock<IGameRepository> GameRepoMock(Game? game)
        {
            var m = new Mock<IGameRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(game);
            return m;
        }

        private static Mock<ISolitaireSessionRepository> SessionRepoMock(GameMatchSession? session)
        {
            var m = new Mock<ISolitaireSessionRepository>();
            m.Setup(x => x.GetByUserGameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
             .ReturnsAsync(session);
            return m;
        }

        private static Mock<IAccountRepository> AccountRepoMock(AccountBasicInfoDTO? info)
        {
            var m = new Mock<IAccountRepository>();
            m.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>())).ReturnsAsync(info);
            return m;
        }

        private static GetSolitaireResultForUserQueryHandler CreateHandler(
            Mock<IGameRepository>?             gameRepo    = null,
            Mock<ISolitaireSessionRepository>? sessionRepo = null,
            Mock<IAccountRepository>?          accountRepo = null)
            => new GetSolitaireResultForUserQueryHandler(
                (gameRepo    ?? GameRepoMock(new Game { GameId = "G1", Status = GameStatus.Active, GameType = GameType.Solitaire })).Object,
                (sessionRepo ?? SessionRepoMock(null)).Object,
                (accountRepo ?? AccountRepoMock(null)).Object);

        private static Game ActiveSolitaire(string id = "G1") =>
            new Game { GameId = id, Status = GameStatus.Active, GameType = GameType.Solitaire };

        private static GetSolitaireResultForUserQuery MakeQuery(string userId = "USER-001") =>
            new GetSolitaireResultForUserQuery { GameId = "G1", UserId = userId, GameDifficulty = GameDifficulty.Easy };

        // TC-SOL-GRU-01 | A | Game not found → 404
        [Fact]
        public async Task Handle_GameNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(gameRepo: GameRepoMock(null)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Solitaire - Get Result For User", new TestCaseDetail { FunctionGroup = "GetSolitaireResultForUser", TestCaseID = "TC-SOL-GRU-01", Description = "Game not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-SOL-GRU-02 | A | Game type mismatch → 400
        [Fact]
        public async Task Handle_GameTypeMismatch_ShouldReturn400()
        {
            var wrongGame = new Game { GameId = "G1", Status = GameStatus.Active, GameType = GameType.MatchingCard };
            var result    = await CreateHandler(gameRepo: GameRepoMock(wrongGame)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Solitaire - Get Result For User", new TestCaseDetail { FunctionGroup = "GetSolitaireResultForUser", TestCaseID = "TC-SOL-GRU-02", Description = "Game is Memory type (not Solitaire) → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GameType != Solitaire" } });
        }

        // TC-SOL-GRU-03 | A | Session not found for user → 404
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(gameRepo: GameRepoMock(ActiveSolitaire()), sessionRepo: SessionRepoMock(null)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Solitaire - Get Result For User", new TestCaseDetail { FunctionGroup = "GetSolitaireResultForUser", TestCaseID = "TC-SOL-GRU-03", Description = "No session for user+game+difficulty → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByUserGameAsync returns null" } });
        }

        // TC-SOL-GRU-04 | N | Happy path → SolitaireResultDto with correct scores
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnDtoWithScores()
        {
            var session = new GameMatchSession { GameMatchSessionId = "S1", UserId = "USER-001", GameId = "G1", BestScore = 300, LatestScore = 150, GameDifficulty = GameDifficulty.Easy };
            var result  = await CreateHandler(
                gameRepo:    GameRepoMock(ActiveSolitaire()),
                sessionRepo: SessionRepoMock(session),
                accountRepo: AccountRepoMock(null)
            ).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.BestScore.Should().Be(300);
            result.Data.LatestScore.Should().Be(150);
            QACollector.LogTestCase("Solitaire - Get Result For User", new TestCaseDetail { FunctionGroup = "GetSolitaireResultForUser", TestCaseID = "TC-SOL-GRU-04", Description = "Happy path → BestScore=300, LatestScore=150", ExpectedResult = "IsSuccess=true, BestScore=300, LatestScore=150", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Session found", "scores mapped" } });
        }

        // TC-SOL-GRU-05 | N | User info enriched → UserName, AvatarUrl mapped
        [Fact]
        public async Task Handle_WithUserInfo_ShouldMapUserNameAndAvatar()
        {
            var session  = new GameMatchSession { GameMatchSessionId = "S1", UserId = "USER-001", GameId = "G1", BestScore = 100, LatestScore = 90, GameDifficulty = GameDifficulty.Easy };
            var userInfo = new AccountBasicInfoDTO { FullName = "Nguyen Van A", AvatarUrl = "https://img/avatar.png" };
            var result   = await CreateHandler(
                gameRepo:    GameRepoMock(ActiveSolitaire()),
                sessionRepo: SessionRepoMock(session),
                accountRepo: AccountRepoMock(userInfo)
            ).Handle(MakeQuery(), CancellationToken.None);
            result.Data!.UserName.Should().Be("Nguyen Van A");
            result.Data.AvatarUrl.Should().Be("https://img/avatar.png");
            QACollector.LogTestCase("Solitaire - Get Result For User", new TestCaseDetail { FunctionGroup = "GetSolitaireResultForUser", TestCaseID = "TC-SOL-GRU-05", Description = "User info enriched → UserName and AvatarUrl mapped", ExpectedResult = "UserName='Nguyen Van A', AvatarUrl set", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "AccountBasicInfoDTO available" } });
        }

        // TC-SOL-GRU-06 | N | User info null → UserName = empty string, AvatarUrl = null
        [Fact]
        public async Task Handle_WithNullUserInfo_ShouldUseEmptyFallbacks()
        {
            var session = new GameMatchSession { GameMatchSessionId = "S1", UserId = "USER-001", GameId = "G1", BestScore = 100, LatestScore = 80, GameDifficulty = GameDifficulty.Easy };
            var result  = await CreateHandler(
                gameRepo:    GameRepoMock(ActiveSolitaire()),
                sessionRepo: SessionRepoMock(session),
                accountRepo: AccountRepoMock(null)
            ).Handle(MakeQuery(), CancellationToken.None);
            result.Data!.UserName.Should().Be(string.Empty);
            result.Data.AvatarUrl.Should().BeNull();
            QACollector.LogTestCase("Solitaire - Get Result For User", new TestCaseDetail { FunctionGroup = "GetSolitaireResultForUser", TestCaseID = "TC-SOL-GRU-06", Description = "No user info → UserName=empty, AvatarUrl=null (fallback)", ExpectedResult = "UserName='', AvatarUrl=null", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "AccountBasicInfoDTO = null", "null coalescing applied" } });
        }
    }
}
