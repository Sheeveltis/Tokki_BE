using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class CreateTopicCommandHandlerTests : TopicTestBase
    {
        private readonly CreateTopicCommandHandler _handler;

        public CreateTopicCommandHandlerTests()
        {
            _handler = new CreateTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockIdGen.Object,
                _mockCreateTopicLogger.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return401_When_Unauthorized()
        {
            SetupUnauthenticatedUser();

            var cmd = TopicTestData.BuildCreateTopicCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_Should_Return409_When_TopicNameDuplicated()
        {
            var cmd = TopicTestData.BuildCreateTopicCommand(name: "Dup");

            _mockTopicRepo
             .Setup(x => x.IsTopicNameExistsAsync("Dup", It.IsAny<string?>()))
             .ReturnsAsync(true);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Handle_Should_Return201_When_Success()
        {
            var cmd = TopicTestData.BuildCreateTopicCommand(name: "New Topic");

            _mockTopicRepo
                .Setup(x => x.IsTopicNameExistsAsync("New Topic", (string?)null))
                .ReturnsAsync(false);
            _mockIdGen.Setup(x => x.GenerateCustom(15)).Returns("topic-99");

            _mockTopicRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Topic>())).Returns(Task.CompletedTask);
            _mockTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("topic-99");

            _mockTopicRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Topic>()), Times.Once);
            _mockTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
