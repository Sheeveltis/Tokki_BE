using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Application.UseCases.Topics.Queries.GetTopicForUser;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Queries
{
    public class GetAllTopicsForUserQueryHandlerTests : TopicTestBase
    {
        private readonly Mock<IUserTopicProgressRepository> _mockProgressRepo;
        private readonly GetAllTopicsForUserQueryHandler _handler;

        public GetAllTopicsForUserQueryHandlerTests()
        {
            _mockProgressRepo = new Mock<IUserTopicProgressRepository>();

            _handler = new GetAllTopicsForUserQueryHandler(
                _mockTopicRepo.Object,
                _mockProgressRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedTopics_WithVocabularyCount_And_Progress_When_UserIdProvided()
        {
            // Arrange
            var q = TopicTestData.BuildGetAllForUserQuery(page: 1, size: 2);
            q.UserId = "user-01";

            var t1 = TopicTestData.BuildTopic(topicId: "t1", topicName: "T1");
            var t2 = TopicTestData.BuildTopic(topicId: "t2", topicName: "T2");

            _mockTopicRepo.Setup(x => x.GetPagedForUserAsync(1, 2, q.SearchTerm, q.Level))
                .ReturnsAsync((new List<Topic> { t1, t2 }, totalCount: 5));

            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t1")).ReturnsAsync(3);
            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t2")).ReturnsAsync(0);

            // t1 learned 1/3 => 33%
            _mockTopicRepo.Setup(x => x.CountLearnedVocabulariesAsync("user-01", "t1")).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(q, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Get the topic list successfully");

            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(5);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(2);

            // item 1: t1
            result.Data.Items[0].TopicId.Should().Be("t1");
            result.Data.Items[0].VocabularyCount.Should().Be(3);
            result.Data.Items[0].Progress.Should().Be(33);
            result.Data.Items[0].IsLearned.Should().BeFalse();

            // item 2: t2 (totalVocab = 0 => progress stays 0, not learned)
            result.Data.Items[1].TopicId.Should().Be("t2");
            result.Data.Items[1].VocabularyCount.Should().Be(0);
            result.Data.Items[1].Progress.Should().Be(0);
            result.Data.Items[1].IsLearned.Should().BeFalse();

            _mockTopicRepo.Verify(x => x.CountVocabulariesInTopicAsync("t1"), Times.Once);
            _mockTopicRepo.Verify(x => x.CountVocabulariesInTopicAsync("t2"), Times.Once);

            // learned count only called when UserId is present AND totalVocab > 0
            _mockTopicRepo.Verify(x => x.CountLearnedVocabulariesAsync("user-01", "t1"), Times.Once);
            _mockTopicRepo.Verify(x => x.CountLearnedVocabulariesAsync("user-01", "t2"), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnProgressZero_And_NotCallLearnedCount_When_UserIdMissing()
        {
            // Arrange
            var q = TopicTestData.BuildGetAllForUserQuery(page: 1, size: 1);
            q.UserId = ""; // không có user => không tính progress

            var t1 = TopicTestData.BuildTopic(topicId: "t1", topicName: "T1");

            _mockTopicRepo.Setup(x => x.GetPagedForUserAsync(1, 1, q.SearchTerm, q.Level))
                .ReturnsAsync((new List<Topic> { t1 }, totalCount: 1));

            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t1")).ReturnsAsync(10);

            // Act
            var result = await _handler.Handle(q, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Get the topic list successfully");

            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(1);

            result.Data.Items[0].TopicId.Should().Be("t1");
            result.Data.Items[0].VocabularyCount.Should().Be(10);
            result.Data.Items[0].Progress.Should().Be(0);
            result.Data.Items[0].IsLearned.Should().BeFalse();

            _mockTopicRepo.Verify(x => x.CountVocabulariesInTopicAsync("t1"), Times.Once);

            // UserId rỗng => KHÔNG gọi learned count
            _mockTopicRepo.Verify(x => x.CountLearnedVocabulariesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_SetIsLearnedTrue_When_ProgressIs100()
        {
            // Arrange
            var q = TopicTestData.BuildGetAllForUserQuery(page: 1, size: 1);
            q.UserId = "user-01";

            var t1 = TopicTestData.BuildTopic(topicId: "t1", topicName: "T1");

            _mockTopicRepo.Setup(x => x.GetPagedForUserAsync(1, 1, q.SearchTerm, q.Level))
                .ReturnsAsync((new List<Topic> { t1 }, totalCount: 1));

            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t1")).ReturnsAsync(3);
            _mockTopicRepo.Setup(x => x.CountLearnedVocabulariesAsync("user-01", "t1")).ReturnsAsync(3);

            // Act
            var result = await _handler.Handle(q, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);

            result.Data.Items[0].VocabularyCount.Should().Be(3);
            result.Data.Items[0].Progress.Should().Be(100);
            result.Data.Items[0].IsLearned.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_CapProgressAt100_When_LearnedGreaterThanTotal()
        {
            // Arrange
            var q = TopicTestData.BuildGetAllForUserQuery(page: 1, size: 1);
            q.UserId = "user-01";

            var t1 = TopicTestData.BuildTopic(topicId: "t1", topicName: "T1");

            _mockTopicRepo.Setup(x => x.GetPagedForUserAsync(1, 1, q.SearchTerm, q.Level))
                .ReturnsAsync((new List<Topic> { t1 }, totalCount: 1));

            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t1")).ReturnsAsync(3);
            _mockTopicRepo.Setup(x => x.CountLearnedVocabulariesAsync("user-01", "t1")).ReturnsAsync(999);

            // Act
            var result = await _handler.Handle(q, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);

            result.Data.Items[0].Progress.Should().Be(100);
            result.Data.Items[0].IsLearned.Should().BeTrue();
        }
    }
}
