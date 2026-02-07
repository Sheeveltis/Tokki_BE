using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.UseCases.Topic.Queries
{
    public class GetTopicByIdQueryHandlerTests : TopicTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TopicExists()
        {
            // Arrange
            var query = new Tokki.Application.UseCases.Topics.Queries.GetById.GetTopicByIdQuery
            {
                TopicId = "topic-123"
            };

            var fakeTopic = TopicTestData.GetFakeTopicEntity();

            _mockRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                     .ReturnsAsync(fakeTopic);

            // Act
            var result = await _getByIdHandler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data.TopicId.Should().Be(fakeTopic.TopicId);
            result.Data.TopicName.Should().Be(fakeTopic.TopicName);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TopicNotFound()
        {
            // Arrange
            var query = new Tokki.Application.UseCases.Topics.Queries.GetById.GetTopicByIdQuery
            {
                TopicId = "non-existent-id"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(query.TopicId))
                     .ReturnsAsync((Tokki.Domain.Entities.Topic)null);

            // Act
            var result = await _getByIdHandler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "Topic.NotFound");
        }
    }
}
