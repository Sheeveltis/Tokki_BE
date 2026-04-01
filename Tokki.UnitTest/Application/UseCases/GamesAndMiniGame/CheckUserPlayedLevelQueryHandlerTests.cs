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
using Tokki.Application.UseCases.Games.Queries.CheckUserPlayedLevel;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class CheckUserPlayedLevelQueryHandlerTests
    {
        private static CheckUserPlayedLevelQueryHandler CreateHandler(
            Mock<IGameMatchSessionRepository>? sessionRepo = null,
            Mock<IHttpContextAccessor>? httpCtx = null)
        {
            return new CheckUserPlayedLevelQueryHandler(
                (sessionRepo ?? new Mock<IGameMatchSessionRepository>()).Object,
                (httpCtx ?? new Mock<IHttpContextAccessor>()).Object,
                new Mock<ILogger<CheckUserPlayedLevelQueryHandler>>().Object);
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

        private static CheckUserPlayedLevelQuery ValidQuery => new()
        {
            GameId         = "GAME-001",
            TopicId        = "TOPIC-001",
            GameDifficulty = GameDifficulty.Easy
        };

        [Fact]
        public async Task Handle_NoUserIdInToken_ShouldReturn401()
        {
            var result = await CreateHandler(httpCtx: BuildHttpCtx(null))
                             .Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckUserPlayedLevel",
                TestCaseID        = "TC-GAME-CPL-01",
                Description       = "No NameIdentifier in JWT → 401 Unauthorized",
                ExpectedResult    = "Return 401",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "currentUserId == null" }
            });
        }

        [Fact]
        public async Task Handle_SessionExists_ShouldReturnTrue()
        {
            var existingSession = new GameMatchSession { BestScore = 100 };
            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ReturnsAsync(existingSession);

            var result = await CreateHandler(sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckUserPlayedLevel",
                TestCaseID        = "TC-GAME-CPL-02",
                Description       = "Session exists for user/game/topic/difficulty → hasPlayed = true",
                ExpectedResult    = "Return 200, Data = true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session != null => hasPlayed = true" }
            });
        }

        [Fact]
        public async Task Handle_SessionNull_ShouldReturnFalse()
        {
            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ReturnsAsync((GameMatchSession?)null);

            var result = await CreateHandler(sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckUserPlayedLevel",
                TestCaseID        = "TC-GAME-CPL-03",
                Description       = "No session found for combination → hasPlayed = false",
                ExpectedResult    = "Return 200, Data = false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session == null => hasPlayed = false" }
            });
        }

        [Fact]
        public async Task Handle_DifferentTopicSameGame_ShouldReturnFalse()
        {
            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    "USER-001", "GAME-001", "TOPIC-002", GameDifficulty.Easy))
                           .ReturnsAsync((GameMatchSession?)null);

            var query = new CheckUserPlayedLevelQuery
            {
                GameId = "GAME-001",
                TopicId = "TOPIC-002",
                GameDifficulty = GameDifficulty.Easy
            };

            var result = await CreateHandler(sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckUserPlayedLevel",
                TestCaseID        = "TC-GAME-CPL-04",
                Description       = "Query for different topic → session not found → hasPlayed=false",
                ExpectedResult    = "Return 200, Data = false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TOPIC-002 not in sessions => false" }
            });
        }

        [Fact]
        public async Task Handle_DifferentDifficulty_ShouldReturnFalse()
        {
            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    "USER-001", "GAME-001", "TOPIC-001", GameDifficulty.Hard))
                           .ReturnsAsync((GameMatchSession?)null);

            var query = new CheckUserPlayedLevelQuery
            {
                GameId = "GAME-001",
                TopicId = "TOPIC-001",
                GameDifficulty = GameDifficulty.Hard
            };

            var result = await CreateHandler(sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckUserPlayedLevel",
                TestCaseID        = "TC-GAME-CPL-05",
                Description       = "Querying for Hard difficulty but only Easy session exists → false",
                ExpectedResult    = "Return 200, Data = false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GameDifficulty.Hard not played yet => false" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            var mockSessionRepo = new Mock<IGameMatchSessionRepository>();
            mockSessionRepo.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                           .ThrowsAsync(new Exception("Timeout"));

            var result = await CreateHandler(sessionRepo: mockSessionRepo, httpCtx: BuildHttpCtx("USER-001"))
                             .Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Games - Check Played Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckUserPlayedLevel",
                TestCaseID        = "TC-GAME-CPL-06",
                Description       = "Repository throws → ServerError 500",
                ExpectedResult    = "Return 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception) => Failure(ServerError, 500)" }
            });
        }
    }
}
