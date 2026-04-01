using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.ApproveTopic;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class ApproveTopicCommandHandlerTests : TopicTestBase
    {
        private readonly ApproveTopicCommandHandler _handler;

        public ApproveTopicCommandHandlerTests()
        {
            _handler = new ApproveTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockAccountRepo.Object,
                _mockEmailService.Object,
                _mockHttpContextAccessor.Object,
                _mockApproveLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return401_When_Unauthorized()
        {
            SetupUnauthenticatedUser();

            var cmd = TopicTestData.BuildApproveCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var cmd = TopicTestData.BuildApproveCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync((Tokki.Domain.Entities.Topic?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_Return400_When_TopicDeleted()
        {
            var cmd = TopicTestData.BuildApproveCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(status: TopicStatus.Deleted));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_Return200_When_AlreadyActive()
        {
            var cmd = TopicTestData.BuildApproveCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(status: TopicStatus.Active));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("has been approved");
        }

        [Fact]
        public async Task Handle_Should_ApproveAndSendEmail_When_PendingApproval()
        {
            var cmd = TopicTestData.BuildApproveCommand();

            var topic = TopicTestData.BuildTopic(status: TopicStatus.PendingApproval, createBy: "creator-01");
            topic.TopicName = "Topic X";

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
            topic.Status.Should().Be(TopicStatus.Active);

            _mockEmailService.Verify(x => x.SendEmailAsync("c@t.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
