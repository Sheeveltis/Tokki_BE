using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Queries.CheckTopicCompletion;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class CheckTopicCompletionQueryHandlerTests
    {
        private CheckTopicCompletionQueryHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null)
        {
            return new CheckTopicCompletionQueryHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var query = new CheckTopicCompletionQuery
            {
                TopicId = "TOPIC-INVALID",
                UserId = "USER-001"
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail
            {
                FunctionGroup = "Check Topic Completion",
                TestCaseID = "TC-TOPIC-CMP-01",
                Description = "Kiểm tra completion với TopicId không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid TopicId",
                    "Topic = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_TopicHasNoVocab_ShouldReturnCompletedWith100Percent()
        {
            // Topic không có vocab → coi như hoàn thành 100%
            var query = new CheckTopicCompletionQuery
            {
                TopicId = "TOPIC-001",
                UserId = "USER-001"
            };

            var mockTopicRepo = MockTopicRepository.GetMock(
                returnedTopic: MockTopicRepository.GetSampleTopic());

            mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>()))
                         .ReturnsAsync(0); // totalVocab = 0

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.IsCompleted.Should().BeTrue();
            result.Data.ProgressPercent.Should().Be(100);
            result.Data.TotalVocab.Should().Be(0);
            result.Data.LearnedVocab.Should().Be(0);

            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail
            {
                FunctionGroup = "Check Topic Completion",
                TestCaseID = "TC-TOPIC-CMP-02",
                Description = "Topic không có vocab → IsCompleted = true, ProgressPercent = 100",
                ExpectedResult = "Return 200, IsCompleted = true, ProgressPercent = 100",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TotalVocab = 0 (boundary: topic rỗng)",
                    "IsCompleted = true",
                    "ProgressPercent = 100"
                }
            });
        }

        [Fact]
        public async Task Handle_UserLearnedAllVocab_ShouldReturnCompletedAndReturn200()
        {
            var query = new CheckTopicCompletionQuery
            {
                TopicId = "TOPIC-001",
                UserId = "USER-001"
            };

            var mockTopicRepo = MockTopicRepository.GetMock(
                returnedTopic: MockTopicRepository.GetSampleTopic());

            mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>()))
                         .ReturnsAsync(10); // totalVocab = 10

            mockTopicRepo.Setup(x => x.CountLearnedVocabulariesAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                         .ReturnsAsync(10); // learnedCount = 10 → 100%

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.IsCompleted.Should().BeTrue();
            result.Data.ProgressPercent.Should().Be(100);
            result.Data.TotalVocab.Should().Be(10);
            result.Data.LearnedVocab.Should().Be(10);
            result.Message.Should().Contain("hoàn thành");

            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail
            {
                FunctionGroup = "Check Topic Completion",
                TestCaseID = "TC-TOPIC-CMP-03",
                Description = "User học hết toàn bộ vocab của topic → IsCompleted = true, 100%",
                ExpectedResult = "Return 200, IsCompleted = true, ProgressPercent = 100",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TotalVocab = 10",
                    "LearnedVocab = 10 (boundary: học hết)",
                    "IsCompleted = true",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_UserLearnedPartialVocab_ShouldReturnProgressPercent()
        {
            var query = new CheckTopicCompletionQuery
            {
                TopicId = "TOPIC-001",
                UserId = "USER-001"
            };

            var mockTopicRepo = MockTopicRepository.GetMock(
                returnedTopic: MockTopicRepository.GetSampleTopic());

            mockTopicRepo.Setup(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>()))
                         .ReturnsAsync(10);

            mockTopicRepo.Setup(x => x.CountLearnedVocabulariesAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                         .ReturnsAsync(5); // learned 5/10 → 50%

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.IsCompleted.Should().BeFalse();
            result.Data.ProgressPercent.Should().Be(50);
            result.Data.LearnedVocab.Should().Be(5);
            result.Message.Should().Contain("chưa hoàn thành");

            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail
            {
                FunctionGroup = "Check Topic Completion",
                TestCaseID = "TC-TOPIC-CMP-04",
                Description = "User học 5/10 vocab → IsCompleted = false, ProgressPercent = 50",
                ExpectedResult = "Return 200, IsCompleted = false, ProgressPercent = 50",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TotalVocab = 10",
                    "LearnedVocab = 5",
                    "ProgressPercent = 50",
                    "IsCompleted = false"
                }
            });
        }
    }
}