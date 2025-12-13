
using Moq;
using Tokki.Application.UseCases.Word.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using FluentAssertions;

namespace Tokki.UnitTests.UseCases.Word.Queries
{
    public class GetWordMeaningsQueryHandlerTests : WordTestBase
    {
        private readonly GetWordMeaningsQueryHandler _handler;

        public GetWordMeaningsQueryHandlerTests()
        {
            _handler = new GetWordMeaningsQueryHandler(
                _mockWordRepo.Object,
                _mockMeaningRepo.Object,
                _mockMeaningTopicRepo.Object,
                _mockTopicRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NoWordIdOrTextProvided()
        {
            // Arrange
            var query = new GetWordMeaningsQuery
            {
                WordId = null,
                Text = null
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == "INVALID_INPUT");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_WordNotFoundById()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordMeaningsQuery();
            _mockWordRepo.Setup(x => x.GetByIdAsync(query.WordId!))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "WORD_NOT_FOUND");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_WordNotFoundByText()
        {
            // Arrange
            var query = new GetWordMeaningsQuery
            {
                Text = "존재하지않는단어",
                PageNumber = 1,
                PageSize = 10
            };
            _mockWordRepo.Setup(x => x.GetByTextAsync(query.Text))
                .ReturnsAsync((Tokki.Domain.Entities.Word)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_ReturnMeanings_When_WordFoundById()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordMeaningsQuery();
            var word = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();
            var meaningTopics = new List<MeaningTopic>
            {
                new MeaningTopic
                {
                    MeaningId = meanings[0].MeaningId,
                    TopicId = "TOPIC-123",
                    Status = MeaningTopicStatus.Active
                }
            };
            var topic = WordTestData.GetFakeTopic();

            _mockWordRepo.Setup(x => x.GetByIdAsync(query.WordId!))
                .ReturnsAsync(word);
            _mockMeaningRepo.Setup(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, query.TopicId, query.Status))
                .ReturnsAsync((meanings, meanings.Count));
            _mockMeaningTopicRepo.Setup(x => x.GetByMeaningIdAsync(It.IsAny<string>()))
                .ReturnsAsync(meaningTopics);
            _mockTopicRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(topic);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(meanings.Count);
            result.Data.Items.Should().OnlyContain(m => m.WordId == word.WordId);
        }

        [Fact]
        public async Task Handle_Should_ReturnMeanings_When_WordFoundByText()
        {
            // Arrange
            var query = new GetWordMeaningsQuery
            {
                Text = "안녕하세요",
                PageNumber = 1,
                PageSize = 10
            };
            var word = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();

            _mockWordRepo.Setup(x => x.GetByTextAsync(query.Text))
                .ReturnsAsync(word);
            _mockMeaningRepo.Setup(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, null, null))
                .ReturnsAsync((meanings, meanings.Count));
            _mockMeaningTopicRepo.Setup(x => x.GetByMeaningIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<MeaningTopic>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(meanings.Count);
            _mockWordRepo.Verify(x => x.GetByTextAsync(query.Text), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_WordHasNoMeanings()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordMeaningsQuery();
            var word = WordTestData.GetFakeWord();

            _mockWordRepo.Setup(x => x.GetByIdAsync(query.WordId!))
                .ReturnsAsync(word);
            _mockMeaningRepo.Setup(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, query.TopicId, query.Status))
                .ReturnsAsync((new List<Meaning>(), 0));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            result.Message.Should().Contain("chưa có nghĩa nào");
        }

        [Fact]
        public async Task Handle_Should_FilterByTopic_When_TopicIdProvided()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordMeaningsQuery();
            query.TopicId = "TOPIC-123";
            var word = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();

            _mockWordRepo.Setup(x => x.GetByIdAsync(query.WordId!))
                .ReturnsAsync(word);
            _mockMeaningRepo.Setup(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, query.TopicId, query.Status))
                .ReturnsAsync((meanings, meanings.Count));
            _mockMeaningTopicRepo.Setup(x => x.GetByMeaningIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<MeaningTopic>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockMeaningRepo.Verify(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, "TOPIC-123", query.Status),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_FilterByStatus_When_StatusProvided()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordMeaningsQuery();
            query.Status = MeaningStatus.Active;
            var word = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();

            _mockWordRepo.Setup(x => x.GetByIdAsync(query.WordId!))
                .ReturnsAsync(word);
            _mockMeaningRepo.Setup(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, query.TopicId, query.Status))
                .ReturnsAsync((meanings.Where(m => m.Status == MeaningStatus.Active).ToList(),
                              meanings.Count(m => m.Status == MeaningStatus.Active)));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockMeaningRepo.Verify(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, query.TopicId, MeaningStatus.Active),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_IncludeTopicsForEachMeaning()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordMeaningsQuery();
            var word = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();
            var meaningTopics = new List<MeaningTopic>
            {
                new MeaningTopic
                {
                    MeaningId = meanings[0].MeaningId,
                    TopicId = "TOPIC-123",
                    Status = MeaningTopicStatus.Active
                }
            };
            var topic = WordTestData.GetFakeTopic();

            _mockWordRepo.Setup(x => x.GetByIdAsync(query.WordId!))
                .ReturnsAsync(word);
            _mockMeaningRepo.Setup(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, query.TopicId, query.Status))
                .ReturnsAsync((meanings, meanings.Count));
            _mockMeaningTopicRepo.Setup(x => x.GetByMeaningIdAsync(It.IsAny<string>()))
                .ReturnsAsync(meaningTopics);
            _mockTopicRepo.Setup(x => x.GetByIdAsync("TOPIC-123"))
                .ReturnsAsync(topic);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().OnlyContain(m => m.Topics.Any());
            result.Data.Items.First().Topics.Should().Contain(t => t.TopicId == "TOPIC-123");
            _mockMeaningTopicRepo.Verify(x => x.GetByMeaningIdAsync(It.IsAny<string>()),
                Times.Exactly(meanings.Count));
        }

        [Fact]
        public async Task Handle_Should_HandlePagination_Correctly()
        {
            // Arrange
            var query = WordTestData.GetValidGetWordMeaningsQuery();
            query.PageNumber = 2;
            query.PageSize = 5;
            var word = WordTestData.GetFakeWord();
            var meanings = WordTestData.GetFakeMeanings();

            _mockWordRepo.Setup(x => x.GetByIdAsync(query.WordId!))
                .ReturnsAsync(word);
            _mockMeaningRepo.Setup(x => x.GetPagedMeaningsByWordIdAsync(
                word.WordId, query.PageNumber, query.PageSize, query.TopicId, query.Status))
                .ReturnsAsync((meanings, 10)); // Total count = 10
            _mockMeaningTopicRepo.Setup(x => x.GetByMeaningIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<MeaningTopic>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(5);
            result.Data.TotalCount.Should().Be(10);
            result.Data.TotalPages.Should().Be(2);
        }
    }
}
