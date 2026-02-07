using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Topics.Queries
{
    public class GetAllTopicsQueryHandlerTests : TopicTestBase
    {
        private readonly GetAllTopicsQueryHandler _handler;

        public GetAllTopicsQueryHandlerTests()
        {
            _handler = new GetAllTopicsQueryHandler(_mockTopicRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnPagedTopics_WithCounts()
        {
            var q = TopicTestData.BuildGetAllTopicsQuery(page: 1, size: 2, status: TopicStatus.Active);

            var t1 = TopicTestData.BuildTopic(topicId: "t1", topicName: "T1", status: TopicStatus.Active);
            var t2 = TopicTestData.BuildTopic(topicId: "t2", topicName: "T2", status: TopicStatus.Active);

            _mockTopicRepo.Setup(x => x.GetPagedAsync(1, 2, q.SearchTerm, q.Status, q.Level))
                .ReturnsAsync((new List<Topic> { t1, t2 }, totalCount: 2));

            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t1")).ReturnsAsync(1);
            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t2")).ReturnsAsync(4);

            var result = await _handler.Handle(q, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(2);
            result.Data.Items[0].VocabularyCount.Should().Be(1);
            result.Data.Items[1].VocabularyCount.Should().Be(4);
        }
    }
}
