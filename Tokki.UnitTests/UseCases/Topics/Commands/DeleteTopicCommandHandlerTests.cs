using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Commands
{
    public class DeleteTopicCommandHandlerTests : TopicTestBase
    {
        private readonly DeleteTopicCommandHandler _handler;

        public DeleteTopicCommandHandlerTests()
        {
            _handler = new DeleteTopicCommandHandler(
                _mockTopicRepo.Object,
                _mockVocabTopicRepo.Object,
                _mockHttpContextAccessor.Object,
                _mockDeleteTopicLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return401_When_Unauthorized()
        {
            SetupUnauthenticatedUser();

            var cmd = TopicTestData.BuildDeleteCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var cmd = TopicTestData.BuildDeleteCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync((Tokki.Domain.Entities.Topic?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_Return400_When_AlreadyDeleted()
        {
            var cmd = TopicTestData.BuildDeleteCommand();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId))
                .ReturnsAsync(TopicTestData.BuildTopic(status: TopicStatus.Deleted));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_SoftDeleteTopic_And_Mappings()
        {
            var cmd = TopicTestData.BuildDeleteCommand();

            var topic = TopicTestData.BuildTopic(status: TopicStatus.Active);
            _mockTopicRepo.Setup(x => x.GetByIdAsync(cmd.TopicId)).ReturnsAsync(topic);

            _mockTopicRepo.Setup(x => x.UpdateAsync(topic)).Returns(Task.CompletedTask);

            var vt1 = TopicTestData.BuildVocabularyTopic(cmd.TopicId, TopicTestData.BuildVocabulary("v1"), VocabularyTopicStatus.Active);
            var vt2 = TopicTestData.BuildVocabularyTopic(cmd.TopicId, TopicTestData.BuildVocabulary("v2"), VocabularyTopicStatus.Active);

            _mockVocabTopicRepo.Setup(x => x.GetByTopicIdAsync(cmd.TopicId))
                .ReturnsAsync(new List<Tokki.Domain.Entities.VocabularyTopic> { vt1, vt2 });

            _mockVocabTopicRepo.Setup(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.VocabularyTopic>()))
                .Returns(Task.CompletedTask);

            _mockTopicRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockVocabTopicRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(1));
            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            topic.Status.Should().Be(TopicStatus.Deleted);
            vt1.Status.Should().Be(VocabularyTopicStatus.Deleted);
            vt2.Status.Should().Be(VocabularyTopicStatus.Deleted);

            _mockTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockVocabTopicRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
