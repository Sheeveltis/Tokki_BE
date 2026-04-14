using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Solitaire.Queries.GetSolitaireResultsForAllUsers;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Solitaire
{
    public class GetSolitaireResultsForAllUsersQueryHandlerTests
    {
        private static Mock<IGameRepository> GameRepoMock(Game? game)
        {
            var m = new Mock<IGameRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(game);
            return m;
        }

        private static Mock<ISolitaireSessionRepository> SessionRepoMock(
            IReadOnlyList<GameMatchSession>? items = null, int total = 0)
        {
            var m = new Mock<ISolitaireSessionRepository>();
            m.Setup(x => x.GetPagedByGameAsync(
                    It.IsAny<string>(), It.IsAny<GameDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
             .ReturnsAsync((items ?? new List<GameMatchSession>().AsReadOnly(), total));
            return m;
        }

        private static Mock<IAccountRepository> AccountRepoMock(AccountBasicInfoDTO? info = null)
        {
            var m = new Mock<IAccountRepository>();
            m.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>())).ReturnsAsync(info);
            return m;
        }

        private static GetSolitaireResultsForAllUsersQueryHandler CreateHandler(
            Mock<IGameRepository>?             gameRepo    = null,
            Mock<ISolitaireSessionRepository>? sessionRepo = null,
            Mock<IAccountRepository>?          accountRepo = null)
            => new GetSolitaireResultsForAllUsersQueryHandler(
                (gameRepo    ?? GameRepoMock(new Game { GameId = "G1", Status = GameStatus.Active, GameType = GameType.Solitaire })).Object,
                (sessionRepo ?? SessionRepoMock()).Object,
                (accountRepo ?? AccountRepoMock()).Object);

        private static Game ActiveSolitaire() => new Game { GameId = "G1", Status = GameStatus.Active, GameType = GameType.Solitaire };

        private static GetSolitaireResultsForAllUsersQuery MakeQuery(int page = 1, int size = 10)
            => new GetSolitaireResultsForAllUsersQuery { GameId = "G1", GameDifficulty = GameDifficulty.Easy, PageNumber = page, PageSize = size };

        // TC-SOL-GRA-01 | A | Game not found → 404
        [Fact]
        public async Task Handle_GameNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(gameRepo: GameRepoMock(null)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Solitaire - Get All Results", new TestCaseDetail { FunctionGroup = "GetSolitaireResultsForAllUsers", TestCaseID = "TC-SOL-GRA-01", Description = "Game not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-SOL-GRA-02 | A | Game type is not Solitaire → 400
        [Fact]
        public async Task Handle_WrongGameType_ShouldReturn400()
        {
            var wrong  = new Game { GameId = "G1", Status = GameStatus.Active, GameType = GameType.MatchingCard };
            var result = await CreateHandler(gameRepo: GameRepoMock(wrong)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Solitaire - Get All Results", new TestCaseDetail { FunctionGroup = "GetSolitaireResultsForAllUsers", TestCaseID = "TC-SOL-GRA-02", Description = "GameType = Memory (not Solitaire) → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GameType != Solitaire" } });
        }

        // TC-SOL-GRA-03 | N | Happy path: 2 sessions → PagedResult with 2 items, 200
        [Fact]
        public async Task Handle_TwoSessions_ShouldReturnPagedResultWith2Items()
        {
            var sessions = new List<GameMatchSession>
            {
                new GameMatchSession { GameMatchSessionId = "S1", UserId = "U1", GameId = "G1", BestScore = 200, LatestScore = 200, GameDifficulty = GameDifficulty.Easy },
                new GameMatchSession { GameMatchSessionId = "S2", UserId = "U2", GameId = "G1", BestScore = 100, LatestScore = 100, GameDifficulty = GameDifficulty.Easy }
            };
            var sessionRepo = SessionRepoMock(sessions.AsReadOnly(), total: 2);
            var result      = await CreateHandler(gameRepo: GameRepoMock(ActiveSolitaire()), sessionRepo: sessionRepo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            QACollector.LogTestCase("Solitaire - Get All Results", new TestCaseDetail { FunctionGroup = "GetSolitaireResultsForAllUsers", TestCaseID = "TC-SOL-GRA-03", Description = "2 sessions → PagedResult.Items.Count=2, TotalCount=2", ExpectedResult = "IsSuccess=true, Items.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "2 sessions returned from repo" } });
        }

        // TC-SOL-GRA-04 | N | UserInfo enriched via cache → each unique userId fetched once
        [Fact]
        public async Task Handle_TwoSessionsSameUser_AccountRepoCalledOnce()
        {
            var sessions = new List<GameMatchSession>
            {
                new GameMatchSession { GameMatchSessionId = "S1", UserId = "U1", GameId = "G1", BestScore = 200, LatestScore = 200, GameDifficulty = GameDifficulty.Easy },
                new GameMatchSession { GameMatchSessionId = "S2", UserId = "U1", GameId = "G1", BestScore = 150, LatestScore = 150, GameDifficulty = GameDifficulty.Easy }
            };
            var sessionRepo = SessionRepoMock(sessions.AsReadOnly(), total: 2);
            var accountRepo = AccountRepoMock(new AccountBasicInfoDTO { FullName = "User One" });
            await CreateHandler(gameRepo: GameRepoMock(ActiveSolitaire()), sessionRepo: sessionRepo, accountRepo: accountRepo).Handle(MakeQuery(), CancellationToken.None);
            // Same UserId → GetBasicInfoAsync should only be called ONCE (cache applied)
            accountRepo.Verify(x => x.GetBasicInfoAsync("U1"), Times.Once);
            QACollector.LogTestCase("Solitaire - Get All Results", new TestCaseDetail { FunctionGroup = "GetSolitaireResultsForAllUsers", TestCaseID = "TC-SOL-GRA-04", Description = "2 sessions same UserId → GetBasicInfoAsync called once (cache)", ExpectedResult = "GetBasicInfoAsync Times.Once", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "userId cache prevents duplicate calls" } });
        }

        // TC-SOL-GRA-05 | N | Empty sessions → 200 with empty PagedResult
        [Fact]
        public async Task Handle_NoSessions_ShouldReturn200WithEmptyPage()
        {
            var sessionRepo = SessionRepoMock(new List<GameMatchSession>().AsReadOnly(), total: 0);
            var result      = await CreateHandler(gameRepo: GameRepoMock(ActiveSolitaire()), sessionRepo: sessionRepo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            QACollector.LogTestCase("Solitaire - Get All Results", new TestCaseDetail { FunctionGroup = "GetSolitaireResultsForAllUsers", TestCaseID = "TC-SOL-GRA-05", Description = "No sessions → 200 with empty page", ExpectedResult = "IsSuccess=true, Items=[], TotalCount=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No records", "empty paged result" } });
        }

        // TC-SOL-GRA-06 | B | Paging params passed correctly to GetPagedByGameAsync
        [Fact]
        public async Task Handle_WithPaging_GetPagedByGameCalledWithCorrectParams()
        {
            var sessionRepo = SessionRepoMock(new List<GameMatchSession>().AsReadOnly(), 0);
            await CreateHandler(gameRepo: GameRepoMock(ActiveSolitaire()), sessionRepo: sessionRepo)
                .Handle(new GetSolitaireResultsForAllUsersQuery { GameId = "G1", GameDifficulty = GameDifficulty.Hard, PageNumber = 2, PageSize = 20 }, CancellationToken.None);
            sessionRepo.Verify(x => x.GetPagedByGameAsync("G1", GameDifficulty.Hard, 2, 20), Times.Once);
            QACollector.LogTestCase("Solitaire - Get All Results", new TestCaseDetail { FunctionGroup = "GetSolitaireResultsForAllUsers", TestCaseID = "TC-SOL-GRA-06", Description = "Paging params passed correctly: GameId, Difficulty, Page=2, Size=20", ExpectedResult = "GetPagedByGameAsync('G1', Hard, 2, 20) Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "PageNumber=2, PageSize=20 forwarded to repo" } });
        }
    }
}
