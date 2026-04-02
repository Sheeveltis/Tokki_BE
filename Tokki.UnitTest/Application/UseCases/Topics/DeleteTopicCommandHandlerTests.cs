using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class DeleteTopicCommandHandlerTests
    {
        private DeleteTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null,
            bool unauthorized = false)
        {
            return new DeleteTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<DeleteTopicCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var command = new DeleteTopicCommand { TopicId = "TOPIC-INVALID" };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Topic",
                TestCaseID = "TC-TOPIC-DEL-01",
                Description = "Xóa topic với ID không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_TopicAlreadyDeleted_ShouldReturn400()
        {
            var command = new DeleteTopicCommand { TopicId = "TOPIC-004" };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopicDeleted()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Topic",
                TestCaseID = "TC-TOPIC-DEL-02",
                Description = "Xóa topic đã bị xóa trước đó (Status = Deleted)",
                ExpectedResult = "Return 400 TopicAlreadyDeleted",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted", "Return 400" }
            });
        }

        [Fact]
        public async Task Handle_ValidTopic_ShouldSoftDeleteCascadeMappingsAndReturn200()
        {
            var command = new DeleteTopicCommand { TopicId = "TOPIC-001" };

            var topic = MockTopicRepository.GetSampleTopic();

            var mappings = MockVocabularyTopicRepository.GetSampleActiveTopicMappings(
                topicId: "TOPIC-001");

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock(
                returnedByTopicId: mappings);

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: topic),
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.Deleted);
            mappings.Should().OnlyContain(m => m.Status == VocabularyTopicStatus.Deleted);

            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Topic",
                TestCaseID = "TC-TOPIC-DEL-03",
                Description = "Xóa topic hợp lệ → soft delete cascade xuống VocabularyTopic mappings",
                ExpectedResult = "Topic + Mappings đều Status = Deleted, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid TopicId",
                    "Has VocabularyTopic mappings",
                    "Cascade soft delete",
                    "Return 200"
                }
            });
        }
    }
}