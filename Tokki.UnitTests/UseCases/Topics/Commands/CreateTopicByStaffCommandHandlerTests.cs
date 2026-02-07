using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class CreateTopicByStaffCommandHandlerTests : TopicTestBase
    {
        private readonly CreateTopicByStaffCommandHandler _handler;

        public CreateTopicByStaffCommandHandlerTests()
        {
            _handler = new CreateTopicByStaffCommandHandler(
                _mockTopicRepo.Object,
                _mockIdGen.Object,
                _mockHttpContextAccessor.Object,
                _mockCreateTopicByStaffLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return401_When_Unauthorized()
        {
            SetupUnauthenticatedUser();

            var cmd = TopicTestData.BuildCreateTopicByStaffCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_Should_Return409_When_NameDuplicated()
        {
            // Arrange
            var cmd = TopicTestData.BuildCreateTopicByStaffCommand(name: "Dup");

            _mockTopicRepo
                .Setup(x => x.IsTopicNameExistsAsync("Dup", (string?)null))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            _mockTopicRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Topic>()), Times.Never);
            _mockTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return201_When_Success()
        {
            var cmd = TopicTestData.BuildCreateTopicByStaffCommand(name: "Staff Topic");

            _mockTopicRepo
                .Setup(x => x.IsTopicNameExistsAsync("Staff Topic", (string?)null))
                .ReturnsAsync(false);
            _mockIdGen.Setup(x => x.GenerateCustom(15)).Returns("topic-77");

            _mockTopicRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Topic>())).Returns(Task.CompletedTask);
            _mockTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("topic-77");
        }
    }
}
