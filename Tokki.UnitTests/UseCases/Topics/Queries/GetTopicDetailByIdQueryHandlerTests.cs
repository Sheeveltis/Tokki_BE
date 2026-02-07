using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Queries
{
    public class GetTopicDetailByIdQueryHandlerTests : TopicTestBase
    {
        private readonly GetTopicDetailByIdQueryHandler _handler;

        public GetTopicDetailByIdQueryHandlerTests()
        {
            _handler = new GetTopicDetailByIdQueryHandler(
                _mockTopicRepo.Object,
                _mockVocabTopicRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return404_When_TopicNotFound()
        {
            var q = TopicTestData.BuildGetDetailQuery();

            _mockTopicRepo.Setup(x => x.GetByIdAsync(q.TopicId)).ReturnsAsync((Topic?)null);

            var result = await _handler.Handle(q, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Handle_Should_FilterOnlyActiveMappings_And_ActiveVocab()
        {
            var q = TopicTestData.BuildGetDetailQuery();

            var topic = TopicTestData.BuildTopic(topicId: q.TopicId, topicName: "T1", status: TopicStatus.Active);
            _mockTopicRepo.Setup(x => x.GetByIdAsync(q.TopicId)).ReturnsAsync(topic);

            var v1 = TopicTestData.BuildVocabulary("v1", VocabularyStatus.Active);
            var v2 = TopicTestData.BuildVocabulary("v2", VocabularyStatus.Deleted); // bị filter
            var v3 = TopicTestData.BuildVocabulary("v3", VocabularyStatus.Active);

            var vt1 = TopicTestData.BuildVocabularyTopic(topic.TopicId, v1, VocabularyTopicStatus.Active);
            var vt2 = TopicTestData.BuildVocabularyTopic(topic.TopicId, v2, VocabularyTopicStatus.Active); // vocab deleted
            var vt3 = TopicTestData.BuildVocabularyTopic(topic.TopicId, v3, VocabularyTopicStatus.Deleted); // mapping deleted

            _mockVocabTopicRepo.Setup(x => x.GetByTopicIdAsync(topic.TopicId))
                .ReturnsAsync(new List<VocabularyTopic> { vt1, vt2, vt3 });

            var result = await _handler.Handle(q, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            result.Data.VocabularyCount.Should().Be(1);
            result.Data.Vocabularies.Should().HaveCount(1);
            result.Data.Vocabularies[0].VocabularyId.Should().Be("v1");
        }
    }
}
