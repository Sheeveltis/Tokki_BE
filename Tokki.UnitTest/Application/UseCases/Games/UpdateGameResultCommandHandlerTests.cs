using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.Commands.UpdateGameResult;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Games
{
    public class UpdateGameResultCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static UpdateGameResultCommandHandler CreateHandler(
            Mock<IGameRepository>? gameRepo = null,
            Mock<IGameMatchSessionRepository>? sessionRepo = null,
            Mock<IHttpContextAccessor>? httpCtx = null)
        {
            return new UpdateGameResultCommandHandler(
                (gameRepo ?? new Mock<IGameRepository>()).Object,
                (sessionRepo ?? new Mock<IGameMatchSessionRepository>()).Object,
                (httpCtx ?? new Mock<IHttpContextAccessor>()).Object,
                new Mock<ILogger<UpdateGameResultCommandHandler>>().Object);
        }

        private static Mock<IHttpContextAccessor> BuildHttpCtx(string? userId)
        {
            var mock = new Mock<IHttpContextAccessor>();
            if (userId == null)
            {
                mock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            }
            else
            {
                var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
                var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
                mock.Setup(x => x.HttpContext).Returns(ctx);
            }
            return mock;
        }

        private static Game ActiveGame() => new() { GameId = "GAME-001", Status = GameStatus.Active, ImgUrl = "img.png" };

        private static UpdateGameResultCommand ValidCommand => new()
        {
            GameId         = "GAME-001",
            TopicId        = "TOPIC-001",
            Score          = 200,
            GameDifficulty = GameDifficulty.Hard
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-UGR-01 | 401 | No userId in token → Unauthorized
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoUserIdInToken_ShouldReturn401()
        {
            var result = await CreateHandler(httpCtx: BuildHttpCtx(null))
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Games - Update Result", new TestCaseDetail
            {
                FunctionGroup     = "UpdateGameResult",
                TestCaseID        = "TC-GAME-UGR-01",
                Description       = "Missing NameIdentifier claim → Unauthorized",
                ExpectedResult    = "Return 401",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "currentUserId == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-UGR-02 | 404 | Game not found → GameNotFound
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_GameNotFound_ShouldReturn404()
        {
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Game?)null);

            var result = await CreateHandler(gameRepo: mockGameRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Games - Update Result", new TestCaseDetail
            {
                FunctionGroup     = "UpdateGameResult",
                TestCaseID        = "TC-GAME-UGR-02",
                Description       = "Game not found in repository",
                ExpectedResult    = "Return 404 GameNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "game == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-UGR-03 | 404 | Session not found → GameResultNotFound
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404GameResultNotFound()
        {
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(ActiveGame());

            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ReturnsAsync((GameMatchSession?)null);

            var result = await CreateHandler(
                gameRepo: mockGameRepo, sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001")
            ).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Games - Update Result", new TestCaseDetail
            {
                FunctionGroup     = "UpdateGameResult",
                TestCaseID        = "TC-GAME-UGR-03",
                Description       = "Session not found → GameResultNotFound (must SaveFirst before Update)",
                ExpectedResult    = "Return 404 GameResultNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session == null => Failure(GameResultNotFound, 404)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-UGR-04 | 200 | Score higher → BestScore updated
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ScoreHigher_ShouldUpdateBestScore()
        {
            var existing = new GameMatchSession { BestScore = 50, LatestScore = 50 };

            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(ActiveGame());

            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ReturnsAsync(existing);
            mockSessionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(
                gameRepo: mockGameRepo, sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001")
            ).Handle(ValidCommand, CancellationToken.None);

            // Score=200 from ValidCommand > BestScore=50
            result.IsSuccess.Should().BeTrue();
            existing.BestScore.Should().Be(200);
            existing.LatestScore.Should().Be(200);

            QACollector.LogTestCase("Games - Update Result", new TestCaseDetail
            {
                FunctionGroup     = "UpdateGameResult",
                TestCaseID        = "TC-GAME-UGR-04",
                Description       = "New score (200) > existing BestScore (50) → BestScore = 200",
                ExpectedResult    = "Return 200, BestScore updated to 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Score > session.BestScore" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-UGR-05 | 200 | Score lower → BestScore unchanged
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ScoreLower_ShouldNotUpdateBestScore()
        {
            var existing = new GameMatchSession { BestScore = 500, LatestScore = 500 };

            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(ActiveGame());

            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ReturnsAsync(existing);
            mockSessionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Score = 200, BestScore = 500
            var result = await CreateHandler(
                gameRepo: mockGameRepo, sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001")
            ).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            existing.BestScore.Should().Be(500); // unchanged
            existing.LatestScore.Should().Be(200); // updated to new

            QACollector.LogTestCase("Games - Update Result", new TestCaseDetail
            {
                FunctionGroup     = "UpdateGameResult",
                TestCaseID        = "TC-GAME-UGR-05",
                Description       = "New score (200) < existing BestScore (500) → BestScore unchanged",
                ExpectedResult    = "Return 200, BestScore stays 500, LatestScore = 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Score <= session.BestScore => BestScore unchanged" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-UGR-06 | 500 | Repository throws → ServerError
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Connection lost"));

            var result = await CreateHandler(gameRepo: mockGameRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Games - Update Result", new TestCaseDetail
            {
                FunctionGroup     = "UpdateGameResult",
                TestCaseID        = "TC-GAME-UGR-06",
                Description       = "Repository throws exception → ServerError 500",
                ExpectedResult    = "Return 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception) => Failure(ServerError, 500)" }
            });
        }
    }
}
