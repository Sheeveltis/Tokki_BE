using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class UpdateTopicCommandHandlerTests : TopicTestBase
    {
        private readonly UpdateTopicCommandHandler _handler;

        public UpdateTopicCommandHandlerTests()
        {
            _handler = new UpdateTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockVocabTopicRepo.Object,
                _mockUpdateTopicLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var cmd = TopicTestData.BuildUpdateTopicCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync((Topic?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_Return409_When_TopicAlreadyDeleted()
        {
            var cmd = TopicTestData.BuildUpdateTopicCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(status: TopicStatus.Deleted));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Handle_Should_Return409_When_NameDuplicated()
        {
            var cmd = TopicTestData.BuildUpdateTopicCommand(topicName: "DupName");

            var topic = TopicTestData.BuildTopic(status: TopicStatus.Draft);
            topic.TopicName = "Old";

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync(topic);
            _mockTopicRepo.Setup(x => x.IsTopicNameExistsAsync("DupName", cmd.TopicId)).ReturnsAsync(true);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Handle_Should_CascadeDeleteMappings_When_StatusSetDeleted()
        {
            var cmd = TopicTestData.BuildUpdateTopicCommand(status: TopicStatus.Deleted, updatedBy: "u1");

            var topic = TopicTestData.BuildTopic(status: TopicStatus.Active);
            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync(topic);

            var vt1 = TopicTestData.BuildVocabularyTopic(topic.TopicId, TopicTestData.BuildVocabulary("v1"), VocabularyTopicStatus.Active);
            var vt2 = TopicTestData.BuildVocabularyTopic(topic.TopicId, TopicTestData.BuildVocabulary("v2"), VocabularyTopicStatus.Active);

            _mockVocabTopicRepo.Setup(x => x.GetByTopicIdAsync(topic.TopicId))
                .ReturnsAsync(new List<VocabularyTopic> { vt1, vt2 });

            _mockVocabTopicRepo.Setup(x => x.UpdateAsync(It.IsAny<VocabularyTopic>())).Returns(Task.CompletedTask);
            _mockVocabTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

            _mockTopicRepo.Setup(x => x.UpdateAsync(topic)).Returns(Task.CompletedTask);
            _mockTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            topic.Status.Should().Be(TopicStatus.Deleted);
            vt1.Status.Should().Be(VocabularyTopicStatus.Deleted);
            vt2.Status.Should().Be(VocabularyTopicStatus.Deleted);

            _mockVocabTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
