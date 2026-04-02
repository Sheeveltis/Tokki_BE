using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class UpdateTopicCommandHandlerTests
    {
        private UpdateTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new UpdateTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object,
                new Mock<ILogger<UpdateTopicCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var command = new UpdateTopicCommand
            {
                TopicId = "TOPIC-INVALID",
                TopicName = "Tên mới",
                UpdatedBy = "ADMIN-001"
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Topic",
                TestCaseID = "TC-TOPIC-UPD-01",
                Description = "Update topic với ID không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_TopicNameDuplicated_ShouldReturn409()
        {
            var existingTopic = MockTopicRepository.GetSampleTopic();

            var command = new UpdateTopicCommand
            {
                TopicId = "TOPIC-001",
                TopicName = "Tên đã tồn tại",  // khác tên hiện tại
                UpdatedBy = "ADMIN-001"
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: existingTopic,
                    isTopicNameExists: true));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Topic - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Topic",
                TestCaseID = "TC-TOPIC-UPD-02",
                Description = "Update TopicName thành tên đã tồn tại của topic khác",
                ExpectedResult = "Return 409 TopicNameDuplicated",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "New TopicName trùng topic khác",
                    "Return 409"
                }
            });
        }

        [Fact]
        public async Task Handle_StatusToDeleted_ShouldCascadeMappingsAndReturn200()
        {
            var existingTopic = MockTopicRepository.GetSampleTopic();

            var command = new UpdateTopicCommand
            {
                TopicId = "TOPIC-001",
                Status = TopicStatus.Deleted,
                UpdatedBy = "ADMIN-001"
            };

            var mappings = MockVocabularyTopicRepository.GetSampleActiveTopicMappings(
                topicId: "TOPIC-001");

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock(
                returnedByTopicId: mappings);

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: existingTopic),
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            existingTopic.Status.Should().Be(TopicStatus.Deleted);
            mappings.Should().OnlyContain(m => m.Status == VocabularyTopicStatus.Deleted);

            QACollector.LogTestCase("Topic - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Topic",
                TestCaseID = "TC-TOPIC-UPD-03",
                Description = "Update Status → Deleted → cascade VocabularyTopic mappings thành Deleted",
                ExpectedResult = "Topic + Mappings = Deleted, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status → Deleted",
                    "Cascade VocabularyTopics",
                    "Return 200"
                }
            });
        }
    }
}