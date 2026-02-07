using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class SubmitTopicForApprovalCommandHandlerTests : TopicTestBase
    {
        private readonly SubmitTopicForApprovalCommandHandler _handler;

        public SubmitTopicForApprovalCommandHandlerTests()
        {
            _handler = new SubmitTopicForApprovalCommandHandler(
                _mockTopicRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockSubmitLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return401_When_Unauthorized()
        {
            SetupUnauthenticatedUser();

            var cmd = TopicTestData.BuildSubmitCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var cmd = TopicTestData.BuildSubmitCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync((Tokki.Domain.Entities.Topic?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_Return400_When_NotDraft()
        {
            var cmd = TopicTestData.BuildSubmitCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(status: TopicStatus.Active));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_SetPendingApproval_When_Draft()
        {
            var cmd = TopicTestData.BuildSubmitCommand();

            var topic = TopicTestData.BuildTopic(status: TopicStatus.Draft);

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync(topic);
            _mockTopicRepo.Setup(x => x.UpdateAsync(topic)).Returns(Task.CompletedTask);
            _mockTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.PendingApproval);
        }
    }
}
