using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic.Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class RemoveVocabulariesFromTopicCommandHandlerTests
    {
        private RemoveVocabulariesFromTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new RemoveVocabulariesFromTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object,
                MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new RemoveVocabulariesFromTopicCommandValidator());
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var command = new RemoveVocabulariesFromTopicCommand
            {
                TopicId = "TOPIC-INVALID",
                VocabularyIds = new List<string> { "VOCAB-001" }
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup = "Remove Vocabularies From Topic",
                TestCaseID = "TC-TOPIC-RVT-01",
                Description = "Gỡ vocab khỏi topic với TopicId không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_NoVocabInTopic_ShouldReturn200WithZeroCount()
        {
            var command = new RemoveVocabulariesFromTopicCommand
            {
                TopicId = "TOPIC-001",
                VocabularyIds = new List<string> { "VOCAB-999" }
            };

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock();
            mockVocabTopicRepo.Setup(x => x.SoftRemoveVocabulariesFromTopicAsync(
                        It.IsAny<string>(),
                        It.IsAny<List<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                     .ReturnsAsync((true, 0, new List<string>()));

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(0);

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup = "Remove Vocabularies From Topic",
                TestCaseID = "TC-TOPIC-RVT-02",
                Description = "Gỡ vocab không thuộc topic → removedCount = 0, không có thay đổi",
                ExpectedResult = "Return 200, Data = 0",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "VocabId không có trong topic",
                    "RemovedCount = 0",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturn200WithRemovedCount()
        {
            var command = new RemoveVocabulariesFromTopicCommand
            {
                TopicId = "TOPIC-001",
                VocabularyIds = new List<string> { "VOCAB-001", "VOCAB-002" }
            };

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock();
            mockVocabTopicRepo.Setup(x => x.SoftRemoveVocabulariesFromTopicAsync(
                        It.IsAny<string>(),
                        It.IsAny<List<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                     .ReturnsAsync((true, 2, new List<string>()));

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(2);

            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail
            {
                FunctionGroup = "Remove Vocabularies From Topic",
                TestCaseID = "TC-TOPIC-RVT-03",
                Description = "Gỡ 2 vocab hợp lệ khỏi topic → removedCount = 2",
                ExpectedResult = "Return 200, Data = 2",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 valid VocabularyIds trong topic",
                    "SoftRemove success",
                    "Return 200, Data = 2"
                }
            });
        }
    }
}