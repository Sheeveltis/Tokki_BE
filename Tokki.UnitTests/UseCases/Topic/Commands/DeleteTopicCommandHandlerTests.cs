using FluentAssertions;
using Moq;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;


namespace Tokki.UnitTests.UseCases.Topic.Commands
{
    public class DeleteTopicCommandHandlerTests : TopicTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TopicCanBeDeleted()
        {
            // Arrange
            var command = TopicTestData.GetValidDeleteTopicCommand();
            var existingTopic = TopicTestData.GetFakeTopicEntity();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync(existingTopic);

            _mockRepo.Setup(x => x.HasMeaningsAsync(command.TopicId))
                     .ReturnsAsync(false);

            // Act
            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();

            _mockRepo.Verify(x => x.DeleteAsync(existingTopic), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TopicNotFound()
        {
            // Arrange
            var command = TopicTestData.GetValidDeleteTopicCommand();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync((Tokki.Domain.Entities.Topic)null);

            // Act
            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "Topic.NotFound");

            _mockRepo.Verify(x => x.DeleteAsync(It.IsAny<Tokki.Domain.Entities.Topic>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TopicHasMeanings()
        {
            // Arrange
            var command = TopicTestData.GetValidDeleteTopicCommand();
            var existingTopic = TopicTestData.GetFakeTopicEntity();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync(existingTopic);

            _mockRepo.Setup(x => x.HasMeaningsAsync(command.TopicId))
                     .ReturnsAsync(true);

            // Act
            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == "Topic.HasMeanings");

            _mockRepo.Verify(x => x.DeleteAsync(It.IsAny<Tokki.Domain.Entities.Topic>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DatabaseErrorOccurs()
        {
            // Arrange
            var command = TopicTestData.GetValidDeleteTopicCommand();
            var existingTopic = TopicTestData.GetFakeTopicEntity();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync(existingTopic);

            _mockRepo.Setup(x => x.HasMeaningsAsync(command.TopicId))
                     .ReturnsAsync(false);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new System.Exception("Database error"));

            // Act
            var result = await _deleteHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
        }
    }
}
