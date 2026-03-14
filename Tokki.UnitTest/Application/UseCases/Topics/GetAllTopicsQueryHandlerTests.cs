using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetAllTopicsQueryHandlerTests
    {
        private GetAllTopicsQueryHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null)
        {
            return new GetAllTopicsQueryHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_NoTopics_ShouldReturnEmptyPagedResult()
        {
            var query = new GetAllTopicsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var mockTopicRepo = MockTopicRepository.GetMock();
            mockTopicRepo.Setup(x => x.GetVocabTopicsPagedAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<TopicStatus?>(),
                        It.IsAny<TopicLevel?>()))
                         .ReturnsAsync((new List<Topic>(), 0));

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Topics",
                TestCaseID = "TC-TOPIC-GAL-01",
                Description = "Không có topic nào trong hệ thống → trả về empty paged result",
                ExpectedResult = "Return 200, Items = empty, TotalCount = 0",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No topics in DB",
                    "TotalCount = 0",
                    "Return 200 empty paged result"
                }
            });
        }

        [Fact]
        public async Task Handle_WithTopics_ShouldReturnPagedResultWithVocabCount()
        {
            var query = new GetAllTopicsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var topics = new List<Topic>
            {
                MockTopicRepository.GetSampleTopic("TOPIC-001"),
                MockTopicRepository.GetSampleTopic("TOPIC-002")
            };

            var mockTopicRepo = MockTopicRepository.GetMock();

            mockTopicRepo.Setup(x => x.GetVocabTopicsPagedAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<TopicStatus?>(),
                        It.IsAny<TopicLevel?>()))
                         .ReturnsAsync((topics, 2));

            // CountVocabulariesInTopicAsync trả về 5 vocab cho mỗi topic
            mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>()))
                         .ReturnsAsync(5);

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            result.Data.Items.Should().OnlyContain(t => t.VocabularyCount == 5);

            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Topics",
                TestCaseID = "TC-TOPIC-GAL-02",
                Description = "Có 2 topics mỗi topic có 5 vocab → trả về paged result với VocabularyCount đúng",
                ExpectedResult = "Return 200, Items.Count = 2, VocabularyCount = 5",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 topics",
                    "Each topic has 5 vocab",
                    "Return 200, VocabularyCount mapped correctly"
                }
            });
        }
    }
}