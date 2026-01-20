//using FluentAssertions;
//using Moq;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Tokki.Application.UseCases.Topics.Queries.GetTopicForUser;
//using Tokki.Domain.Entities;
//using Tokki.UnitTests.Common.Bases;
//using Tokki.UnitTests.Common.TestData;
//using Xunit;

//namespace Tokki.UnitTests.Features.Topics.Queries
//{
//    public class GetAllTopicsForUserQueryHandlerTests : TopicTestBase
//    {
//        private readonly GetAllTopicsForUserQueryHandler _handler;

//        public GetAllTopicsForUserQueryHandlerTests()
//        {
//            _handler = new GetAllTopicsForUserQueryHandler(_mockTopicRepo.Object);
//        }

//        [Fact]
//        public async Task Handle_Should_ReturnPagedTopics_WithVocabularyCount()
//        {
//            var q = TopicTestData.BuildGetAllForUserQuery(page: 1, size: 2);

//            var t1 = TopicTestData.BuildTopic(topicId: "t1", topicName: "T1");
//            var t2 = TopicTestData.BuildTopic(topicId: "t2", topicName: "T2");

//            _mockTopicRepo.Setup(x => x.GetPagedForUserAsync(1, 2, q.SearchTerm, q.Level))
//                .ReturnsAsync((new List<Topic> { t1, t2 }, totalCount: 5));

//            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t1")).ReturnsAsync(3);
//            _mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("t2")).ReturnsAsync(0);

//            var result = await _handler.Handle(q, CancellationToken.None);

//            result.IsSuccess.Should().BeTrue();
//            result.StatusCode.Should().Be(200);
//            result.Data.Items.Should().HaveCount(2);
//            result.Data.TotalCount.Should().Be(5);

//            result.Data.Items[0].VocabularyCount.Should().Be(3);
//            result.Data.Items[1].VocabularyCount.Should().Be(0);

//            _mockTopicRepo.Verify(x => x.CountVocabulariesInTopicAsync("t1"), Times.Once);
//            _mockTopicRepo.Verify(x => x.CountVocabulariesInTopicAsync("t2"), Times.Once);
//        }
//    }
//}
