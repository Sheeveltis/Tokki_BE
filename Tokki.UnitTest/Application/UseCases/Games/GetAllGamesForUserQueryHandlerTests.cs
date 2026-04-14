using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.DTOs;
using Tokki.Application.UseCases.Games.Queries.GetAllGamesForUser;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Games
{
    public class GetAllGamesForUserQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static GetAllGamesForUserQueryHandler CreateHandler(
            Mock<IGameRepository>? gameRepo = null)
        {
            return new GetAllGamesForUserQueryHandler(
                (gameRepo ?? new Mock<IGameRepository>()).Object);
        }

        private static Game BuildGame(string id = "G1", string name = "Matching Card", GameStatus status = GameStatus.Active) => new()
        {
            GameId   = id,
            GameName = name,
            GameType = GameType.MatchingCard,
            Status   = status,
            IsVip    = false,
            ImgUrl   = "https://img.example.com/game.png"
        };

        private static GetAllGamesForUserQuery DefaultQuery => new()
        {
            PageNumber  = 1,
            PageSize    = 10,
            SearchTerm  = null,
            GameType    = null
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-GAG-01 | 200 | Empty list → PagedResult empty
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyGames_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetPagedForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<GameType?>()))
                        .ReturnsAsync((new List<Game>().AsReadOnly(), 0));

            // Act
            var result = await CreateHandler(mockGameRepo).Handle(DefaultQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Games - Get All For User", new TestCaseDetail
            {
                FunctionGroup     = "GetAllGamesForUser",
                TestCaseID        = "TC-GAME-GAG-01",
                Description       = "Repository returns empty list → PagedResult with 0 items",
                ExpectedResult    = "Return 200, Items = empty, TotalCount = 0",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "result.Items.Count == 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-GAG-02 | 200 | Valid games → mapped to GameForUserDto correctly
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidGames_ShouldMapToDto()
        {
            // Arrange
            var games = new List<Game> { BuildGame("G1", "Matching Card"), BuildGame("G2", "Quiz") }.AsReadOnly();

            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetPagedForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<GameType?>()))
                        .ReturnsAsync((games, 2));

            // Act
            var result = await CreateHandler(mockGameRepo).Handle(DefaultQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items[0].GameId.Should().Be("G1");
            result.Data.Items[0].GameName.Should().Be("Matching Card");

            QACollector.LogTestCase("Games - Get All For User", new TestCaseDetail
            {
                FunctionGroup     = "GetAllGamesForUser",
                TestCaseID        = "TC-GAME-GAG-02",
                Description       = "2 games returned and mapped to GameForUserDto",
                ExpectedResult    = "Return 200, Items = 2, GameId and GameName mapped correctly",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "games.Count == 2" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-GAG-03 | 200 | Pagination metadata correct
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaginationQuery_ShouldReturnCorrectMetadata()
        {
            // Arrange – 15 total games, page 2, size 5
            var games = new List<Game> { BuildGame(), BuildGame("G2"), BuildGame("G3"), BuildGame("G4"), BuildGame("G5") }.AsReadOnly();

            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetPagedForUserAsync(2, 5, null, null))
                        .ReturnsAsync((games, 15));

            var query = new GetAllGamesForUserQuery { PageNumber = 2, PageSize = 5 };

            // Act
            var result = await CreateHandler(mockGameRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.TotalCount.Should().Be(15);
            result.Data.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(5);
            result.Data.TotalPages.Should().Be(3); // ceil(15/5)

            QACollector.LogTestCase("Games - Get All For User", new TestCaseDetail
            {
                FunctionGroup     = "GetAllGamesForUser",
                TestCaseID        = "TC-GAME-GAG-03",
                Description       = "Page 2/5 of 15 total → pagination metadata correct",
                ExpectedResult    = "TotalPages=3, PageNumber=2, PageSize=5",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PagedResult.Create(items, 15, 2, 5)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-GAG-04 | 200 | SearchTerm filter passed to repository
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithSearchTerm_ShouldPassToRepository()
        {
            // Arrange
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetPagedForUserAsync(1, 10, "Matching", null))
                        .ReturnsAsync((new List<Game> { BuildGame() }.AsReadOnly(), 1));

            var query = new GetAllGamesForUserQuery { PageNumber = 1, PageSize = 10, SearchTerm = "Matching" };

            // Act
            var result = await CreateHandler(mockGameRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockGameRepo.Verify(x => x.GetPagedForUserAsync(1, 10, "Matching", null), Times.Once);

            QACollector.LogTestCase("Games - Get All For User", new TestCaseDetail
            {
                FunctionGroup     = "GetAllGamesForUser",
                TestCaseID        = "TC-GAME-GAG-04",
                Description       = "SearchTerm 'Matching' passed directly to repository",
                ExpectedResult    = "Return 200, GetPagedForUserAsync called with SearchTerm='Matching'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.SearchTerm = 'Matching'" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-GAG-05 | 200 | GameType filter passed to repository
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithGameTypeFilter_ShouldPassToRepository()
        {
            // Arrange
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetPagedForUserAsync(1, 10, null, GameType.MatchingCard))
                        .ReturnsAsync((new List<Game> { BuildGame() }.AsReadOnly(), 1));

            var query = new GetAllGamesForUserQuery { PageNumber = 1, PageSize = 10, GameType = GameType.MatchingCard };

            // Act
            var result = await CreateHandler(mockGameRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockGameRepo.Verify(x => x.GetPagedForUserAsync(1, 10, null, GameType.MatchingCard), Times.Once);

            QACollector.LogTestCase("Games - Get All For User", new TestCaseDetail
            {
                FunctionGroup     = "GetAllGamesForUser",
                TestCaseID        = "TC-GAME-GAG-05",
                Description       = "GameType filter passed directly to repository",
                ExpectedResult    = "Return 200, repo called with GameType.MatchingCard",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.GameType = MatchingCard" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-GAME-GAG-06 | 200 | IsVip flag mapped correctly to DTO
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VipGame_ShouldMapIsVipToDto()
        {
            // Arrange
            var vipGame = new Game
            {
                GameId   = "VIP-001",
                GameName = "VIP Wordle",
                GameType = GameType.MatchingCard,
                Status   = GameStatus.Active,
                IsVip    = true,
                ImgUrl   = "vip.png"
            };
            var mockGameRepo = new Mock<IGameRepository>();
            mockGameRepo.Setup(x => x.GetPagedForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<GameType?>()))
                        .ReturnsAsync((new List<Game> { vipGame }.AsReadOnly(), 1));

            // Act
            var result = await CreateHandler(mockGameRepo).Handle(DefaultQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items[0].IsVip.Should().BeTrue();
            result.Data.Items[0].ImgUrl.Should().Be("vip.png");

            QACollector.LogTestCase("Games - Get All For User", new TestCaseDetail
            {
                FunctionGroup     = "GetAllGamesForUser",
                TestCaseID        = "TC-GAME-GAG-06",
                Description       = "VIP game IsVip=true and ImgUrl properly mapped to DTO",
                ExpectedResult    = "DTO.IsVip = true, DTO.ImgUrl = 'vip.png'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "game.IsVip = true => dto.IsVip = true" }
            });
        }
    }
}
