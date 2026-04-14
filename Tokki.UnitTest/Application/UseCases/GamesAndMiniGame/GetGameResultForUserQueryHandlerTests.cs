using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Games.Queries.GetGameResultForUser;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class GetGameResultForUserQueryHandlerTests
    {
        private static GetGameResultForUserQueryHandler CreateHandler(
            Mock<IGameMatchSessionRepository>? sessionRepo = null,
            Mock<IAccountRepository>? accountRepo = null)
        {
            return new GetGameResultForUserQueryHandler(
                (sessionRepo ?? new Mock<IGameMatchSessionRepository>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object);
        }

        private static GetGameResultForUserQuery ValidQuery => new()
        {
            UserId         = "USER-001",
            GameId         = "GAME-001",
            TopicId        = "TOPIC-001",
            GameDifficulty = GameDifficulty.Easy
        };

        private static GameMatchSession BuildSession(int best = 300, int latest = 200) => new()
        {
            GameMatchSessionId = "SES-001",
            UserId             = "USER-001",
            GameId             = "GAME-001",
            TopicId            = "TOPIC-001",
            BestScore          = best,
            LatestScore        = latest,
            GameDifficulty     = GameDifficulty.Easy,
            CreatedAt          = DateTime.UtcNow
        };

        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                       .ReturnsAsync((GameMatchSession?)null);

            var result = await CreateHandler(sessionRepo: mockSession).Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Games - Get Result For User", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultForUser",
                TestCaseID        = "TC-GAME-GRU-01",
                Description       = "Session not found for given user/game/topic/difficulty",
                ExpectedResult    = "Return 404 GameResultNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session == null" }
            });
        }

        [Fact]
        public async Task Handle_SessionFound_ShouldMapDtoCorrectly()
        {
            var session = BuildSession(300, 200);
            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                       .ReturnsAsync(session);

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync("USER-001"))
                       .ReturnsAsync(new AccountBasicInfoDTO { FullName = "Nguyễn Văn A", AvatarUrl = "avatar.png" });

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.BestScore.Should().Be(300);
            result.Data.LatestScore.Should().Be(200);
            result.Data.UserName.Should().Be("Nguyễn Văn A");
            result.Data.AvatarUrl.Should().Be("avatar.png");

            QACollector.LogTestCase("Games - Get Result For User", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultForUser",
                TestCaseID        = "TC-GAME-GRU-02",
                Description       = "Session + account info found → DTO fully populated",
                ExpectedResult    = "Return 200, BestScore=300, UserName='Nguyễn Văn A'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "session != null, account found" }
            });
        }

        [Fact]
        public async Task Handle_AccountNotFound_ShouldFallbackEmptyUserName()
        {
            var session = BuildSession();
            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                       .ReturnsAsync(session);

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync("USER-001"))
                       .ReturnsAsync((AccountBasicInfoDTO?)null);

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.UserName.Should().Be(string.Empty);
            result.Data.AvatarUrl.Should().BeNull();

            QACollector.LogTestCase("Games - Get Result For User", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultForUser",
                TestCaseID        = "TC-GAME-GRU-03",
                Description       = "Account not found → UserName = string.Empty, AvatarUrl = null",
                ExpectedResult    = "Return 200, UserName = ''",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userInfo == null => UserName = string.Empty" }
            });
        }

        [Fact]
        public async Task Handle_ValidSession_ShouldMapSessionId()
        {
            var session = BuildSession();
            session.GameMatchSessionId = "SES-UNIQUE-999";

            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                       .ReturnsAsync(session);

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>())).ReturnsAsync(new AccountBasicInfoDTO { FullName = "Test" });

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(ValidQuery, CancellationToken.None);

            result.Data!.GameMatchSessionId.Should().Be("SES-UNIQUE-999");

            QACollector.LogTestCase("Games - Get Result For User", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultForUser",
                TestCaseID        = "TC-GAME-GRU-04",
                Description       = "GameMatchSessionId properly mapped to result DTO",
                ExpectedResult    = "DTO.GameMatchSessionId = 'SES-UNIQUE-999'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "dto.GameMatchSessionId = session.GameMatchSessionId" }
            });
        }

        [Fact]
        public async Task Handle_AccountWithTitleInfo_ShouldMapTitleToDto()
        {
            var session = BuildSession();
            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                       .ReturnsAsync(session);

            var accountInfo = new AccountBasicInfoDTO
            {
                FullName             = "Pro Player",
                AvatarUrl            = "avatar.png",
                CurrentTitleName     = "Gold Master",
                CurrentColorHexTitle = "#FFD700",
                TitleIconUrl         = "gold-icon.png"
            };
            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>())).ReturnsAsync(accountInfo);

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(ValidQuery, CancellationToken.None);

            result.Data!.TitleName.Should().Be("Gold Master");
            result.Data.TitleColorHex.Should().Be("#FFD700");
            result.Data.TitleIconUrl.Should().Be("gold-icon.png");

            QACollector.LogTestCase("Games - Get Result For User", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultForUser",
                TestCaseID        = "TC-GAME-GRU-05",
                Description       = "Title fields from AccountBasicInfoDTO mapped to DTO",
                ExpectedResult    = "TitleName='Gold Master', ColorHex='#FFD700'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "info.CurrentTitleName -> dto.TitleName" }
            });
        }

        [Fact]
        public async Task Handle_ValidSession_ShouldMapGameDifficulty()
        {
            var session = new GameMatchSession
            {
                GameMatchSessionId = "S1",
                UserId             = "USER-001",
                GameId             = "GAME-001",
                TopicId            = "TOPIC-001",
                BestScore          = 50,
                LatestScore        = 50,
                GameDifficulty     = GameDifficulty.Hard,
                CreatedAt          = DateTime.UtcNow
            };

            var mockSession = new Mock<IGameMatchSessionRepository>();
            mockSession.Setup(x => x.GetByUserGameTopicAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GameDifficulty>()))
                       .ReturnsAsync(session);

            var mockAccount = new Mock<IAccountRepository>();
            mockAccount.Setup(x => x.GetBasicInfoAsync(It.IsAny<string>())).ReturnsAsync(new AccountBasicInfoDTO { FullName = "Test" });

            var hardQuery = new GetGameResultForUserQuery
            {
                UserId         = "USER-001",
                GameId         = "GAME-001",
                TopicId        = "TOPIC-001",
                GameDifficulty = GameDifficulty.Hard
            };

            var result = await CreateHandler(sessionRepo: mockSession, accountRepo: mockAccount)
                             .Handle(hardQuery, CancellationToken.None);

            result.Data!.GameDifficulty.Should().Be(GameDifficulty.Hard);

            QACollector.LogTestCase("Games - Get Result For User", new TestCaseDetail
            {
                FunctionGroup     = "GetGameResultForUser",
                TestCaseID        = "TC-GAME-GRU-06",
                Description       = "GameDifficulty.Hard properly mapped to result DTO",
                ExpectedResult    = "DTO.GameDifficulty = Hard",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "dto.GameDifficulty = session.GameDifficulty" }
            });
        }
    }
}
