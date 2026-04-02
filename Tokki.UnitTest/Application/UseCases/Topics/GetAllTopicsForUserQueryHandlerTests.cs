using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Application.UseCases.Topics.Queries.GetTopicForUser;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetAllTopicsForUserQueryHandlerTests
    {
        private Mock<IUserTopicProgressRepository> _mockProgressRepo = new Mock<IUserTopicProgressRepository>();

        private GetAllTopicsForUserQueryHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null)
        {
            return new GetAllTopicsForUserQueryHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                _mockProgressRepo.Object);
        }

        [Fact]
        public async Task Handle_NoTopics_ShouldReturnEmptyPagedResult()
        {
            var query = new GetAllTopicsForUserQuery
            {
                UserId = "USER-001",
                PageNumber = 1,
                PageSize = 10
            };

            var mockTopicRepo = MockTopicRepository.GetMock();
            mockTopicRepo.Setup(x => x.GetVocabTopicsPagedForUserAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<TopicLevel?>()))
                         .ReturnsAsync((new List<Topic>(), 0));

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().BeEmpty();

            QACollector.LogTestCase("Topic - Get All For User", new TestCaseDetail
            {
                FunctionGroup = "Get All Topics For User",
                TestCaseID = "TC-TOPIC-GUS-01",
                Description = "Không có topic nào → trả về empty paged result",
                ExpectedResult = "Return 200, Items = empty",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No topics available",
                    "Return 200 empty"
                }
            });
        }

        [Fact]
        public async Task Handle_UserWithProgress_ShouldReturnCorrectProgressPercent()
        {
            var query = new GetAllTopicsForUserQuery
            {
                UserId = "USER-001",
                PageNumber = 1,
                PageSize = 10
            };

            var topics = new List<Topic>
            {
                MockTopicRepository.GetSampleTopic("TOPIC-001")
            };

            var mockTopicRepo = MockTopicRepository.GetMock();
            mockTopicRepo.Setup(x => x.GetVocabTopicsPagedForUserAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<TopicLevel?>()))
                         .ReturnsAsync((topics, 1));

            // 10 vocab, user học 6 → 60%
            mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("TOPIC-001"))
                         .ReturnsAsync(10);

            mockTopicRepo.Setup(x => x.CountLearnedVocabulariesAsync("USER-001", "TOPIC-001"))
                         .ReturnsAsync(6);

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].Progress.Should().Be(60);
            result.Data.Items[0].IsLearned.Should().BeFalse();

            QACollector.LogTestCase("Topic - Get All For User", new TestCaseDetail
            {
                FunctionGroup = "Get All Topics For User",
                TestCaseID = "TC-TOPIC-GUS-02",
                Description = "User học 6/10 vocab → Progress = 60%, IsLearned = false",
                ExpectedResult = "Return 200, Progress = 60, IsLearned = false",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TotalVocab = 10",
                    "LearnedCount = 6",
                    "Progress = 60%",
                    "IsLearned = false",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_UserCompletedTopic_ShouldReturnIsLearnedTrue()
        {
            var query = new GetAllTopicsForUserQuery
            {
                UserId = "USER-001",
                PageNumber = 1,
                PageSize = 10
            };

            var topics = new List<Topic>
            {
                MockTopicRepository.GetSampleTopic("TOPIC-001")
            };

            var mockTopicRepo = MockTopicRepository.GetMock();
            mockTopicRepo.Setup(x => x.GetVocabTopicsPagedForUserAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<TopicLevel?>()))
                         .ReturnsAsync((topics, 1));

            // Học hết 10/10 → 100%
            mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync("TOPIC-001"))
                         .ReturnsAsync(10);

            mockTopicRepo.Setup(x => x.CountLearnedVocabulariesAsync("USER-001", "TOPIC-001"))
                         .ReturnsAsync(10);

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items[0].Progress.Should().Be(100);
            result.Data.Items[0].IsLearned.Should().BeTrue();

            QACollector.LogTestCase("Topic - Get All For User", new TestCaseDetail
            {
                FunctionGroup = "Get All Topics For User",
                TestCaseID = "TC-TOPIC-GUS-03",
                Description = "User học hết 10/10 vocab → Progress = 100%, IsLearned = true",
                ExpectedResult = "Return 200, Progress = 100, IsLearned = true",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TotalVocab = 10",
                    "LearnedCount = 10 (boundary: học hết)",
                    "Progress = 100%",
                    "IsLearned = true"
                }
            });
        }
    }
}