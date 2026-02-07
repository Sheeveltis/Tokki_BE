using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.RejectTopic;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class RejectTopicCommandHandlerTests : TopicTestBase
    {
        private readonly RejectTopicCommandHandler _handler;

        public RejectTopicCommandHandlerTests()
        {
            _handler = new RejectTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockAccountRepo.Object,
                _mockEmailService.Object,
                _mockHttpContextAccessor.Object,
                _mockRejectLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return401_When_Unauthorized()
        {
            SetupUnauthenticatedUser();

            var cmd = TopicTestData.BuildRejectCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_Should_Return400_When_RejectReasonMissing()
        {
            var cmd = TopicTestData.BuildRejectCommand(reason: "   ");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_Return400_When_StatusNotPendingApproval()
        {
            var cmd = TopicTestData.BuildRejectCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(status: TopicStatus.Draft));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_Reject_And_SendEmail()
        {
            var cmd = TopicTestData.BuildRejectCommand(reason: "bad");

            var topic = TopicTestData.BuildTopic(status: TopicStatus.PendingApproval, createBy: "creator-01");
            topic.TopicName = "TReject";

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync(topic);
            _mockTopicRepo.Setup(x => x.UpdateAsync(topic)).Returns(Task.CompletedTask);
            _mockTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var creator = TopicTestData.BuildAccount("creator-01", email: "c@t.com", fullName: "Creator");
            _mockAccountRepo.Setup(x => x.GetByIdAsync("creator-01")).ReturnsAsync(creator);

            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.Rejected);

            _mockEmailService.Verify(x => x.SendEmailAsync("c@t.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
