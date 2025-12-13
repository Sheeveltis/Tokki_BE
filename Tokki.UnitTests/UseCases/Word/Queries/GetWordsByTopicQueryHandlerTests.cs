using Moq;
using Tokki.Application.UseCases.Word.Queries.GetWordsByTopicQuery;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using FluentAssertions;

namespace Tokki.UnitTests.UseCases.Word.Queries
{
    public class GetWordsByTopicQueryHandlerTests : WordTestBase
    {
        private readonly GetWordsByTopicQueryHandler _handler;

        public GetWordsByTopicQueryHandlerTests()
        {
            _handler = new GetWordsByTopicQueryHandler(
                _mockWordRepo.Object,
                _mockMeaningRepo.Object,
                _mockTopicRepo.Object,
                _mockMeaningTopicRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TopicNotFound()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordsByTopicQuery();
            _mockTopicRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                .ReturnsAsync((Tokki.Domain.Entities.Topic)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "TOPIC_NOT_FOUND");
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedWords_When_TopicExists()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordsByTopicQuery();
            var topic = WordTestData.GetFakeTopic();
            var words = WordTestData.GetFakeWordsList();
            var meanings = WordTestData.GetFakeMeanings();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetPagedWordsByTopicIdAsync(
                query.TopicId, query.PageNumber, query.PageSize, query.SearchTerm, query.Status))
                .ReturnsAsync((words, words.Count));
            _mockMeaningRepo.Setup(x => x.GetMeaningsByWordIdAndTopicIdAsync(It.IsAny<string>(), query.TopicId))
                .ReturnsAsync(meanings);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(words.Count);
            result.Data.TotalCount.Should().Be(words.Count);
            result.Data.PageNumber.Should().Be(query.PageNumber);
            result.Data.PageSize.Should().Be(query.PageSize);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_TopicHasNoWords()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordsByTopicQuery();
            var topic = WordTestData.GetFakeTopic();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetPagedWordsByTopicIdAsync(
                query.TopicId, query.PageNumber, query.PageSize, query.SearchTerm, query.Status))
                .ReturnsAsync((new List<Tokki.Domain.Entities.Word>(), 0));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            result.Message.Should().Contain("chưa có từ vựng nào");
        }

        [Fact]
        public async Task Handle_Should_FilterByStatus_When_StatusProvided()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordsByTopicQuery();
            query.Status = WordStatus.Active;
            var topic = WordTestData.GetFakeTopic();
            var words = WordTestData.GetFakeWordsList();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetPagedWordsByTopicIdAsync(
                query.TopicId, query.PageNumber, query.PageSize, query.SearchTerm, query.Status))
                .ReturnsAsync((words.Where(w => w.Status == WordStatus.Active).ToList(),
                              words.Count(w => w.Status == WordStatus.Active)));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().OnlyContain(w => w.Status == WordStatus.Active);
            _mockWordRepo.Verify(x => x.GetPagedWordsByTopicIdAsync(
                query.TopicId, query.PageNumber, query.PageSize, query.SearchTerm, WordStatus.Active),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_FilterBySearchTerm_When_SearchTermProvided()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordsByTopicQuery();
            query.SearchTerm = "안녕";
            var topic = WordTestData.GetFakeTopic();
            var words = WordTestData.GetFakeWordsList().Take(1).ToList();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetPagedWordsByTopicIdAsync(
                query.TopicId, query.PageNumber, query.PageSize, query.SearchTerm, query.Status))
                .ReturnsAsync((words, words.Count));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockWordRepo.Verify(x => x.GetPagedWordsByTopicIdAsync(
                query.TopicId, query.PageNumber, query.PageSize, "안녕", query.Status),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_IncludeMeaningsForEachWord_When_RetrievingWords()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordsByTopicQuery();
            var topic = WordTestData.GetFakeTopic();
            var words = WordTestData.GetFakeWordsList();
            var meanings = WordTestData.GetFakeMeanings();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                .ReturnsAsync(topic);
            _mockWordRepo.Setup(x => x.GetPagedWordsByTopicIdAsync(
                query.TopicId, query.PageNumber, query.PageSize, query.SearchTerm, query.Status))
                .ReturnsAsync((words, words.Count));
            _mockMeaningRepo.Setup(x => x.GetMeaningsByWordIdAndTopicIdAsync(It.IsAny<string>(), query.TopicId))
                .ReturnsAsync(meanings);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().OnlyContain(w => w.Meanings.Any());
            _mockMeaningRepo.Verify(x => x.GetMeaningsByWordIdAndTopicIdAsync(It.IsAny<string>(), query.TopicId),
                Times.Exactly(words.Count));
        }
    }
}
