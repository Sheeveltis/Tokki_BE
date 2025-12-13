using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

    namespace Tokki.UnitTests.UseCases.Topic.Commands
{
    public class CreateTopicCommandHandlerTests : TopicTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            // Arrange
            var command = TopicTestData.GetValidCreateTopicCommand();

            _mockRepo.Setup(x => x.IsTopicNameExistsAsync(command.TopicName, null))
                     .ReturnsAsync(false);

            _mockIdGen.Setup(x => x.GenerateCustom(15))
                     .Returns("topic-new-123");

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("topic-new-123");

            _mockRepo.Verify(x => x.AddAsync(It.Is<Tokki.Domain.Entities.Topic>(t =>
                t.TopicId == "topic-new-123" &&
                t.TopicName == command.TopicName &&
                t.CreateBy == command.CreateBy &&
                t.Status == Domain.Enums.TopicStatus.Active
            )), Times.Once);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TopicNameExists()
        {
            // Arrange
            var command = TopicTestData.GetDuplicateTopicCommand();

            _mockRepo.Setup(x => x.IsTopicNameExistsAsync(command.TopicName, null))
                     .ReturnsAsync(true);

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().Contain(e => e.Code == "Topic.NameDuplicated");

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Topic>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_DatabaseErrorOccurs()
        {
            // Arrange
            var command = TopicTestData.GetValidCreateTopicCommand();

            _mockRepo.Setup(x => x.IsTopicNameExistsAsync(command.TopicName, null))
                     .ReturnsAsync(false);

            _mockIdGen.Setup(x => x.GenerateCustom(15))
                     .Returns("topic-new-123");

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _createHandler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(e => e.Code == "App.ServerError");
        }
    }
}



