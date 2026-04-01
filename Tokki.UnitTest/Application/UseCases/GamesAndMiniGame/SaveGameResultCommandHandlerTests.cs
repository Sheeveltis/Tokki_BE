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
using Tokki.Application.UseCases.Games.Commands.SaveGameResult;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class SaveGameResultCommandHandlerTests
    {
        private static SaveGameResultCommandHandler CreateHandler(
            Mock<IGameRepository>? gameRepo = null,
            Mock<IGameMatchSessionRepository>? sessionRepo = null,
            Mock<IIdGeneratorService>? idGen = null,
            Mock<IHttpContextAccessor>? httpCtx = null)
        {
            return new SaveGameResultCommandHandler(
                (gameRepo ?? new Mock<IGameRepository>()).Object,
                (sessionRepo ?? new Mock<IGameMatchSessionRepository>()).Object,
                (idGen ?? new Mock<IIdGeneratorService>()).Object,
                (httpCtx ?? new Mock<IHttpContextAccessor>()).Object,
                new Mock<ILogger<SaveGameResultCommandHandler>>().Object);
        }

        private static Mock<IHttpContextAccessor> BuildHttpCtx(string? userId)
        {
            var mockCtx = new Mock<IHttpContextAccessor>();
            if (userId == null)
            {
                mockCtx.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            }
            else
            {
                var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
                var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
                mockCtx.Setup(x => x.HttpContext).Returns(httpContext);
            }
            return mockCtx;
        }

        private static Game ActiveGame(string id = "GAME-001") => new Game
        {
            GameId   = id,
            GameName = "Matching Card",
            Status   = GameStatus.Active,
            GameType = GameType.MatchingCard,
            ImgUrl   = "img.png"
        };

        private static SaveGameResultCommand ValidCommand => new()
        {
            GameId         = "GAME-001",
            TopicId        = "TOPIC-001",
            Score          = 100,
            GameDifficulty = GameDifficulty.Easy
        };

        [Fact]
        public async Task Handle_NoUserIdInToken_ShouldReturn401()
        {
            var result = await CreateHandler(httpCtx: BuildHttpCtx(null))
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Games - Save Result", new TestCaseDetail
            {
                FunctionGroup     = "SaveGameResult",
                TestCaseID        = "TC-GAME-SGR-01",
                Description       = "HttpContext has no NameIdentifier claim → UserUnauthorized",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "string.IsNullOrWhiteSpace(currentUserId)" }
            });
        }

        [Fact]
        public async Task Handle_GameNotFound_ShouldReturn404()
        {
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Game?)null);

            var result = await CreateHandler(gameRepo: mockGameRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Games - Save Result", new TestCaseDetail
            {
                FunctionGroup     = "SaveGameResult",
                TestCaseID        = "TC-GAME-SGR-02",
                Description       = "Game does not exist in repository → GameNotFound",
                ExpectedResult    = "Return 404 GameNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "game == null" }
            });
        }

        [Fact]
        public async Task Handle_GameInactive_ShouldReturn404()
        {
            var inactiveGame = new Game { GameId = "GAME-001", Status = GameStatus.Draft, ImgUrl = "img.png" };
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(inactiveGame);

            var result = await CreateHandler(gameRepo: mockGameRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Games - Save Result", new TestCaseDetail
            {
                FunctionGroup     = "SaveGameResult",
                TestCaseID        = "TC-GAME-SGR-03",
                Description       = "Game.Status = Draft (inactive) → GameNotFound error",
                ExpectedResult    = "Return 404 GameNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "game.Status != GameStatus.Active" }
            });
        }

        [Fact]
        public async Task Handle_SessionNull_ShouldCreateNewSession()
        {
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(ActiveGame());

            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ReturnsAsync((GameMatchSession?)null);
            mockSessionRepo.Setup(x => x.AddAsync(It.IsAny<GameMatchSession>())).Returns(Task.CompletedTask);
            mockSessionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.GenerateCustom(15)).Returns("SESSION-001");

            var result = await CreateHandler(
                gameRepo: mockGameRepo, sessionRepo: mockSessionRepo, idGen: mockIdGen, httpCtx: BuildHttpCtx("USER-001")
            ).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            mockSessionRepo.Verify(x => x.AddAsync(It.IsAny<GameMatchSession>()), Times.Once);

            QACollector.LogTestCase("Games - Save Result", new TestCaseDetail
            {
                FunctionGroup     = "SaveGameResult",
                TestCaseID        = "TC-GAME-SGR-04",
                Description       = "No existing session → adds new GameMatchSession",
                ExpectedResult    = "Return 200, AddAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session == null => AddAsync" }
            });
        }

        [Fact]
        public async Task Handle_ExistingSession_HigherScore_ShouldUpdateBestScore()
        {
            var existingSession = new GameMatchSession { BestScore = 80, LatestScore = 80 };

            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(ActiveGame());

            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ReturnsAsync(existingSession);
            mockSessionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new SaveGameResultCommand { GameId = "GAME-001", TopicId = "TOPIC-001", Score = 150, GameDifficulty = GameDifficulty.Easy };

            var result = await CreateHandler(
                gameRepo: mockGameRepo, sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001")
            ).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            existingSession.BestScore.Should().Be(150);
            existingSession.LatestScore.Should().Be(150);

            QACollector.LogTestCase("Games - Save Result", new TestCaseDetail
            {
                FunctionGroup     = "SaveGameResult",
                TestCaseID        = "TC-GAME-SGR-05",
                Description       = "Existing session, new score (150) > BestScore (80) → BestScore updated",
                ExpectedResult    = "Return 200, BestScore = 150",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Score > session.BestScore => BestScore = request.Score" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));

            var result = await CreateHandler(gameRepo: mockGameRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Games - Save Result", new TestCaseDetail
            {
                FunctionGroup     = "SaveGameResult",
                TestCaseID        = "TC-GAME-SGR-06",
                Description       = "Repository throws unhandled exception → ServerError 500",
                ExpectedResult    = "Return 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) => Failure(ServerError, 500)" }
            });
        }
    }
}
