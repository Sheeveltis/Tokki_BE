using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.AddFavoriteVocabulary;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.FavoriteVocabulary.Commands
{
    public class AddFavoriteVocabularyCommandHandlerTests : FavoriteVocabularyTestBase
    {
        private readonly AddFavoriteVocabularyCommandHandler _handler;

        public AddFavoriteVocabularyCommandHandlerTests()
        {
            _handler = new AddFavoriteVocabularyCommandHandler(
                _mockFavoriteRepo.Object,
                _mockVocabularyRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnUnauthorized_When_UserNotLoggedIn()
        {
            // Arrange
            SetupUnauthenticatedUser();

            var command = FavoriteVocabularyTestData.GetValidAddCommand("vocab-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Errors.Should().Contain(e => e.Code == AppErrors.Unauthorized.Code);

            _mockVocabularyRepo.Verify(x => x.GetByIdsAsync(It.IsAny<List<string>>()), Times.Never);
            _mockFavoriteRepo.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockFavoriteRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_VocabularyNotExists()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetValidAddCommand("vocab-01");

            _mockVocabularyRepo
                .Setup(x => x.GetByIdsAsync(It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == command.VocabularyId)))
                .ReturnsAsync(new List<Vocabulary>()); // empty => not found

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.VocabularyNotFound.Code);

            _mockFavoriteRepo.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockFavoriteRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_VocabularyNotActive()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetValidAddCommand("vocab-01");

            var vocab = FavoriteVocabularyTestData.GetActiveVocabulary(command.VocabularyId);
            vocab.Status = (VocabularyStatus)999; // ép giá trị khác Active để đảm bảo nhánh != Active

            _mockVocabularyRepo
                .Setup(x => x.GetByIdsAsync(It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == command.VocabularyId)))
                .ReturnsAsync(new List<Vocabulary> { vocab });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.VocabularyNotFound.Code);

            _mockFavoriteRepo.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockFavoriteRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_FavoriteAlreadyExists()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetValidAddCommand("vocab-01");

            var vocab = FavoriteVocabularyTestData.GetActiveVocabulary(command.VocabularyId);

            _mockVocabularyRepo
                .Setup(x => x.GetByIdsAsync(It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == command.VocabularyId)))
                .ReturnsAsync(new List<Vocabulary> { vocab });

            _mockFavoriteRepo
                .Setup(x => x.ExistsAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Đã tồn tại trong danh sách yêu thích.");

            _mockFavoriteRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockIdGenerator.Verify(x => x.GenerateCustom(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_AddFavorite_And_ReturnSuccess_When_NewFavorite()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetValidAddCommand("vocab-01");

            var vocab = FavoriteVocabularyTestData.GetActiveVocabulary(command.VocabularyId);

            _mockVocabularyRepo
                .Setup(x => x.GetByIdsAsync(It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == command.VocabularyId)))
                .ReturnsAsync(new List<Vocabulary> { vocab });

            _mockFavoriteRepo
                .Setup(x => x.ExistsAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var generatedId = "fav-generated-01";
            _mockIdGenerator
                .Setup(x => x.GenerateCustom(15))
                .Returns(generatedId);

            UserFavoriteVocabulary? captured = null;

            _mockFavoriteRepo
                .Setup(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()))
                .Callback<UserFavoriteVocabulary, CancellationToken>((e, _) => captured = e)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Thêm vào danh sách yêu thích thành công.");

            _mockIdGenerator.Verify(x => x.GenerateCustom(15), Times.Once);
            _mockFavoriteRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Once);

            captured.Should().NotBeNull();
            captured!.FavoriteVocabularyId.Should().Be(generatedId);
            captured.UserId.Should().Be(DefaultUserId);
            captured.VocabularyId.Should().Be(command.VocabularyId);
            captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AddThrows()
        {
            // Arrange
            var command = FavoriteVocabularyTestData.GetValidAddCommand("vocab-01");

            var vocab = FavoriteVocabularyTestData.GetActiveVocabulary(command.VocabularyId);

            _mockVocabularyRepo
                .Setup(x => x.GetByIdsAsync(It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == command.VocabularyId)))
                .ReturnsAsync(new List<Vocabulary> { vocab });

            _mockFavoriteRepo
                .Setup(x => x.ExistsAsync(DefaultUserId, command.VocabularyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockIdGenerator
                .Setup(x => x.GenerateCustom(15))
                .Returns("fav-generated-01");

            _mockFavoriteRepo
                .Setup(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("db error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.FavoriteVocabularyAddFailed.Code);

            _mockFavoriteRepo.Verify(x => x.AddAsync(It.IsAny<UserFavoriteVocabulary>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
