using FluentAssertions;
using Moq;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;

namespace Tokki.UnitTests.UseCases.Topic.Commands
{
    public class UpdateTopicCommandHandlerTests : TopicTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            // Arrange
            var command = TopicTestData.GetValidUpdateTopicCommand();
            var existingTopic = TopicTestData.GetFakeTopicEntity();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync(existingTopic);

            _mockRepo.Setup(x => x.IsTopicNameExistsAsync(command.TopicName, command.TopicId))
                     .ReturnsAsync(false);

            // Act
            var result = await _updateHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();

            _mockRepo.Verify(x => x.UpdateAsync(It.Is<Tokki.Domain.Entities.Topic>(t =>
                t.TopicId == command.TopicId &&
                t.TopicName == command.TopicName &&
                t.UpdateBy == command.UpdatedBy &&
                t.UpdateDate != null
            )), Times.Once);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TopicNotFound()
        {
            // Arrange
            var command = TopicTestData.GetValidUpdateTopicCommand();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync((Tokki.Domain.Entities.Topic)null);

            // Act
            var result = await _updateHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == "Topic.NotFound");

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Topic>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NewNameAlreadyExists()
        {
            // Arrange
            var command = TopicTestData.GetUpdateWithDuplicateNameCommand();
            var existingTopic = TopicTestData.GetFakeTopicEntity();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync(existingTopic);

            _mockRepo.Setup(x => x.IsTopicNameExistsAsync(command.TopicName, command.TopicId))
                     .ReturnsAsync(true);

            // Act
            var result = await _updateHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().Contain(e => e.Code == "Topic.NameDuplicated");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DatabaseErrorOccurs()
        {
            // Arrange
            var command = TopicTestData.GetValidUpdateTopicCommand();
            var existingTopic = TopicTestData.GetFakeTopicEntity();

            _mockRepo.Setup(x => x.GetByIdAsync(command.TopicId))
                     .ReturnsAsync(existingTopic);

            _mockRepo.Setup(x => x.IsTopicNameExistsAsync(command.TopicName, command.TopicId))
                     .ReturnsAsync(false);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new System.Exception("Database error"));

            // Act
            var result = await _updateHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
        }
    }
}
