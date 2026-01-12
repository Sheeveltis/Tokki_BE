using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.FavoriteVocabulary.DTOs;
using Tokki.Application.UseCases.FavoriteVocabulary.Queries.GetFavoriteVocabularies;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.FavoriteVocabulary.Queries
{
    public class GetFavoriteVocabulariesQueryHandlerTests : FavoriteVocabularyTestBase
    {
        private readonly GetFavoriteVocabulariesQueryHandler _handler;

        public GetFavoriteVocabulariesQueryHandlerTests()
        {
            _handler = new GetFavoriteVocabulariesQueryHandler(
                _mockFavoriteRepo.Object,
                _mockTopicRepo.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnUnauthorized_When_UserNotLoggedIn()
        {
            // Arrange
            SetupUnauthenticatedUser();

            var query = FavoriteVocabularyTestData.GetGetFavoritesQuery(
                topicId: null,
                pageNumber: 1,
                pageSize: 10,
                searchTerm: null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Errors.Should().Contain(e => e.Code == AppErrors.Unauthorized.Code);

            _mockTopicRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);

            _mockFavoriteRepo.Verify(x => x.GetPagedByUserAndTopicAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TopicNotFound()
        {
            // Arrange
            var query = FavoriteVocabularyTestData.GetGetFavoritesQuery(topicId: "topic-01");

            _mockTopicRepo
                .Setup(x => x.GetByIdAsync("topic-01"))
                .ReturnsAsync((Topic?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.TopicNotFound.Code);

            _mockFavoriteRepo.Verify(x => x.GetPagedByUserAndTopicAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_TopicNotActive()
        {
            // Arrange
            var query = FavoriteVocabularyTestData.GetGetFavoritesQuery(topicId: "topic-01");

            var topic = FavoriteVocabularyTestData.GetTopicWithStatus(TopicStatus.Draft, "topic-01");

            _mockTopicRepo
                .Setup(x => x.GetByIdAsync("topic-01"))
                .ReturnsAsync(topic);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.TopicNotFound.Code);

            _mockFavoriteRepo.Verify(x => x.GetPagedByUserAndTopicAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TopicIdNull_And_MapPagedResult()
        {
            // Arrange
            var query = FavoriteVocabularyTestData.GetGetFavoritesQuery(
                topicId: null,
                pageNumber: 2,
                pageSize: 5,
                searchTerm: "he");

            var items = new List<UserFavoriteVocabulary>
            {
                BuildFavorite(DefaultUserId, "vocab-01", "hello", "xin chào"),
                BuildFavorite(DefaultUserId, "vocab-02", "help", "giúp đỡ")
            };

            const int totalCount = 12;

            _mockFavoriteRepo
                .Setup(x => x.GetPagedByUserAndTopicAsync(
                    DefaultUserId,
                    null,
                    query.PageNumber,
                    query.PageSize,
                    query.SearchTerm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((items, totalCount));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Lấy danh sách từ vựng yêu thích thành công");

            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);

            result.Data.TotalCount.Should().Be(totalCount);
            result.Data.PageNumber.Should().Be(query.PageNumber);
            result.Data.PageSize.Should().Be(query.PageSize);

            result.Data.Items.First().VocabularyId.Should().Be("vocab-01");
            result.Data.Items.First().Text.Should().Be("hello");
            result.Data.Items.First().Definition.Should().Be("xin chào");

            _mockTopicRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);

            _mockFavoriteRepo.Verify(x => x.GetPagedByUserAndTopicAsync(
                DefaultUserId,
                null,
                query.PageNumber,
                query.PageSize,
                query.SearchTerm,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_TrimTopicId_And_CallRepoWithTrimmedValue()
        {
            // Arrange
            var query = FavoriteVocabularyTestData.GetGetFavoritesQuery(topicId: "  topic-01  ", pageNumber: 1, pageSize: 10, searchTerm: null);

            var topic = FavoriteVocabularyTestData.GetTopicWithStatus(TopicStatus.Active, "topic-01");

            _mockTopicRepo
                .Setup(x => x.GetByIdAsync("topic-01"))
                .ReturnsAsync(topic);

            _mockFavoriteRepo
                .Setup(x => x.GetPagedByUserAndTopicAsync(
                    DefaultUserId,
                    "topic-01",
                    query.PageNumber,
                    query.PageSize,
                    query.SearchTerm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<UserFavoriteVocabulary>(), 0));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            _mockTopicRepo.Verify(x => x.GetByIdAsync("topic-01"), Times.Once);

            _mockFavoriteRepo.Verify(x => x.GetPagedByUserAndTopicAsync(
                DefaultUserId,
                It.Is<string?>(t => t == "topic-01"),
                query.PageNumber,
                query.PageSize,
                query.SearchTerm,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private static UserFavoriteVocabulary BuildFavorite(
            string userId,
            string vocabularyId,
            string text,
            string definition)
        {
            return new UserFavoriteVocabulary
            {
                FavoriteVocabularyId = "fav-" + vocabularyId,
                UserId = userId,
                VocabularyId = vocabularyId,
                CreatedAt = DateTime.UtcNow,
                Vocabulary = new Vocabulary
                {
                    VocabularyId = vocabularyId,
                    Text = text,
                    Definition = definition,
                    Pronunciation = "pron",
                    ImgURL = "img",
                    AudioURL = "audio",
                    Status = VocabularyStatus.Active
                }
            };
        }
    }
}
