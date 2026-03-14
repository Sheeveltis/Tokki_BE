using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopicStatus;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class UpdateTopicStatusCommandHandlerTests
    {
        private UpdateTopicStatusCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new UpdateTopicStatusCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object,
                new Mock<ILogger<UpdateTopicStatusCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_TopicAlreadyDeleted_ShouldReturn409()
        {
            var command = new UpdateTopicStatusCommand
            {
                TopicId = "TOPIC-004",
                Status = TopicStatus.Active,
                UpdatedBy = "ADMIN-001"
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopicDeleted()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Topic - UpdateStatus", new TestCaseDetail
            {
                FunctionGroup = "Update Topic Status",
                TestCaseID = "TC-TOPIC-STS-01",
                Description = "Update Status của topic đã bị xóa (Status = Deleted)",
                ExpectedResult = "Return 409 TopicAlreadyDeleted",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted", "Return 409" }
            });
        }

        [Fact]
        public async Task Handle_StatusToDeleted_ShouldCascadeMappingsAndReturn200()
        {
            var existingTopic = MockTopicRepository.GetSampleTopic();

            var command = new UpdateTopicStatusCommand
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

            QACollector.LogTestCase("Topic - UpdateStatus", new TestCaseDetail
            {
                FunctionGroup = "Update Topic Status",
                TestCaseID = "TC-TOPIC-STS-02",
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

        [Fact]
        public async Task Handle_SameStatus_ShouldSkipCascadeAndReturn200()
        {
            // Status không đổi → không cascade, vẫn update audit
            var existingTopic = MockTopicRepository.GetSampleTopic(status: TopicStatus.Active);

            var command = new UpdateTopicStatusCommand
            {
                TopicId = "TOPIC-001",
                Status = TopicStatus.Active, // same
                UpdatedBy = "ADMIN-001"
            };

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock();

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: existingTopic),
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // GetByTopicIdAsync không được gọi vì status không thay đổi
            mockVocabTopicRepo.Verify(
                x => x.GetByTopicIdAsync(It.IsAny<string>()),
                Times.Never);

            QACollector.LogTestCase("Topic - UpdateStatus", new TestCaseDetail
            {
                FunctionGroup = "Update Topic Status",
                TestCaseID = "TC-TOPIC-STS-03",
                Description = "Update Status với giá trị giống hiện tại → không cascade, return 200",
                ExpectedResult = "GetByTopicIdAsync không gọi, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "NewStatus == OldStatus = Active (boundary: no change)",
                    "No cascade",
                    "Return 200"
                }
            });
        }
    }
}