using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Solitaire.Commands.SaveSolitaireResult;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Solitaire
{
    public class SaveSolitaireResultCommandHandlerTests
    {
        // ── Mock helpers ──────────────────────────────────────────────────
        private static Mock<IHttpContextAccessor> GetHttpMock(string? userId)
        {
            var mock = new Mock<IHttpContextAccessor>();
            if (userId == null)
            {
                mock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            }
            else
            {
                var claims  = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
                var identity = new ClaimsIdentity(claims);
                var principal = new ClaimsPrincipal(identity);
                var ctx      = new DefaultHttpContext { User = principal };
                mock.Setup(x => x.HttpContext).Returns(ctx);
            }
            return mock;
        }

        private static Mock<IGameRepository> GetGameRepoMock(Game? game)
        {
            var m = new Mock<IGameRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(game);
            return m;
        }

        private static Mock<ISolitaireSessionRepository> GetSessionRepoMock(GameMatchSession? session = null)
        {
            var m = new Mock<ISolitaireSessionRepository>();
            m.Setup(x => x.GetByUserGameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
             .ReturnsAsync(session);
            m.Setup(x => x.AddAsync(It.IsAny<GameMatchSession>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static SaveSolitaireResultCommandHandler CreateHandler(
            Mock<IGameRepository>?              gameRepo    = null,
            Mock<ISolitaireSessionRepository>?  sessionRepo = null,
            Mock<IHttpContextAccessor>?         httpCtx     = null,
            string?                             userId      = "USER-001")
        {
            var idGen  = new Mock<IIdGeneratorService>();
            idGen.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("SESSION-001");
            var logger = new Mock<ILogger<SaveSolitaireResultCommandHandler>>();

            return new SaveSolitaireResultCommandHandler(
                (gameRepo    ?? GetGameRepoMock(new Game { GameId = "G1", Status = GameStatus.Active, GameType = GameType.Solitaire })).Object,
                (sessionRepo ?? GetSessionRepoMock()).Object,
                idGen.Object,
                (httpCtx     ?? GetHttpMock(userId)).Object,
                logger.Object);
        }

        private static Game ActiveSolitaire(string id = "G1") =>
            new Game { GameId = id, Status = GameStatus.Active, GameType = GameType.Solitaire };

        private static SaveSolitaireResultCommand MakeCommand(string gameId = "G1") =>
            new SaveSolitaireResultCommand { GameId = gameId, Score = 100, GameDifficulty = GameDifficulty.Easy };

        // TC-SOL-SAVE-01 | A | No authenticated user → 401
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(httpCtx: GetHttpMock(null)).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Solitaire - Save Result", new TestCaseDetail { FunctionGroup = "SaveSolitaireResult", TestCaseID = "TC-SOL-SAVE-01", Description = "No auth user (null HttpContext) → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "HttpContext = null" } });
        }

        // TC-SOL-SAVE-02 | A | Game not found → 404
        [Fact]
        public async Task Handle_GameNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(gameRepo: GetGameRepoMock(null)).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Solitaire - Save Result", new TestCaseDetail { FunctionGroup = "SaveSolitaireResult", TestCaseID = "TC-SOL-SAVE-02", Description = "Game not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-SOL-SAVE-03 | A | Game is not Solitaire type → 400
        [Fact]
        public async Task Handle_GameIsNotSolitaire_ShouldReturn400()
        {
            var wrongTypeGame = new Game { GameId = "G1", Status = GameStatus.Active, GameType = GameType.MatchingCard };
            var result = await CreateHandler(gameRepo: GetGameRepoMock(wrongTypeGame)).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Solitaire - Save Result", new TestCaseDetail { FunctionGroup = "SaveSolitaireResult", TestCaseID = "TC-SOL-SAVE-03", Description = "Game is Memory type (not Solitaire) → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GameType != Solitaire" } });
        }

        // TC-SOL-SAVE-04 | N | First time playing → new session created (AddAsync called)
        [Fact]
        public async Task Handle_NoExistingSession_ShouldCreateNewSession()
        {
            var sessionRepo = GetSessionRepoMock(session: null); // no existing session
            var result      = await CreateHandler(gameRepo: GetGameRepoMock(ActiveSolitaire()), sessionRepo: sessionRepo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            sessionRepo.Verify(x => x.AddAsync(It.IsAny<GameMatchSession>()), Times.Once);
            QACollector.LogTestCase("Solitaire - Save Result", new TestCaseDetail { FunctionGroup = "SaveSolitaireResult", TestCaseID = "TC-SOL-SAVE-04", Description = "No existing session → AddAsync called, 200", ExpectedResult = "IsSuccess=true, AddAsync Times.Once", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No previous session", "new GameMatchSession created" } });
        }

        // TC-SOL-SAVE-05 | N | Existing session with higher new score → BestScore updated
        [Fact]
        public async Task Handle_ExistingSessionWithHigherScore_ShouldUpdateBestScore()
        {
            var existingSession = new GameMatchSession { GameMatchSessionId = "S1", UserId = "USER-001", GameId = "G1", BestScore = 50, LatestScore = 50, GameDifficulty = GameDifficulty.Easy };
            var sessionRepo     = GetSessionRepoMock(session: existingSession);
            var cmd             = new SaveSolitaireResultCommand { GameId = "G1", Score = 200, GameDifficulty = GameDifficulty.Easy };
            await CreateHandler(gameRepo: GetGameRepoMock(ActiveSolitaire()), sessionRepo: sessionRepo).Handle(cmd, CancellationToken.None);
            existingSession.BestScore.Should().Be(200);    // updated
            existingSession.LatestScore.Should().Be(200);  // also updated
            sessionRepo.Verify(x => x.AddAsync(It.IsAny<GameMatchSession>()), Times.Never); // no new session
            QACollector.LogTestCase("Solitaire - Save Result", new TestCaseDetail { FunctionGroup = "SaveSolitaireResult", TestCaseID = "TC-SOL-SAVE-05", Description = "New score(200) > BestScore(50) → BestScore updated, no new session", ExpectedResult = "BestScore=200, AddAsync Times.Never", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Score > BestScore", "BestScore updated" } });
        }

        // TC-SOL-SAVE-06 | N | Existing session with lower new score → BestScore NOT changed, LatestScore updated
        [Fact]
        public async Task Handle_ExistingSessionWithLowerScore_ShouldNotUpdateBestScore()
        {
            var existingSession = new GameMatchSession { GameMatchSessionId = "S1", UserId = "USER-001", GameId = "G1", BestScore = 500, LatestScore = 500, GameDifficulty = GameDifficulty.Easy };
            var sessionRepo     = GetSessionRepoMock(session: existingSession);
            var cmd             = new SaveSolitaireResultCommand { GameId = "G1", Score = 30, GameDifficulty = GameDifficulty.Easy };
            await CreateHandler(gameRepo: GetGameRepoMock(ActiveSolitaire()), sessionRepo: sessionRepo).Handle(cmd, CancellationToken.None);
            existingSession.BestScore.Should().Be(500);   // unchanged
            existingSession.LatestScore.Should().Be(30);  // updated to new score
            QACollector.LogTestCase("Solitaire - Save Result", new TestCaseDetail { FunctionGroup = "SaveSolitaireResult", TestCaseID = "TC-SOL-SAVE-06", Description = "New score(30) < BestScore(500) → BestScore unchanged, LatestScore=30", ExpectedResult = "BestScore=500, LatestScore=30", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Score < BestScore", "only LatestScore updated" } });
        }
    }
}
