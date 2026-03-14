using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Queries.FlashCard;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class FlashCardQueryHandlerTests
    {
        private FlashCardQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new FlashCardQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var query = new FlashCardQuery { TopicId = "TOPIC-INVALID" };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary - FlashCard", new TestCaseDetail
            {
                FunctionGroup = "Get FlashCard",
                TestCaseID = "TC-VOCAB-FC-01",
                Description = "Lấy flashcard với TopicId không tồn tại",
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
        public async Task Handle_TopicHasNoActiveVocab_ShouldReturnEmptyList200()
        {
            var query = new FlashCardQuery { TopicId = "TOPIC-001" };

            // Topic tồn tại nhưng không có VocabularyTopic nào Active
            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(
                    returnedByTopicId: new List<VocabularyTopic>()));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Vocabulary - FlashCard", new TestCaseDetail
            {
                FunctionGroup = "Get FlashCard",
                TestCaseID = "TC-VOCAB-FC-02",
                Description = "Topic tồn tại nhưng chưa có vocab nào → trả về empty list",
                ExpectedResult = "Return 200, Data = empty list",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid TopicId",
                    "No VocabularyTopic relationships",
                    "Return 200 empty list"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidTopic_ShouldReturnOnlyActiveVocabs()
        {
            var query = new FlashCardQuery { TopicId = "TOPIC-001" };

            var vocabTopics = new List<VocabularyTopic>
            {
                new VocabularyTopic
                {
                    VocabularyId = "VOCAB-001",
                    TopicId = "TOPIC-001",
                    Status = VocabularyTopicStatus.Active
                },
                new VocabularyTopic
                {
                    VocabularyId = "VOCAB-002",
                    TopicId = "TOPIC-001",
                    Status = VocabularyTopicStatus.Deleted // bị loại
                }
            };

            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active)
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                         .ReturnsAsync(vocabs);

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(
                    returnedByTopicId: vocabTopics));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(1);
            result.Data[0].VocabularyId.Should().Be("VOCAB-001");

            QACollector.LogTestCase("Vocabulary - FlashCard", new TestCaseDetail
            {
                FunctionGroup = "Get FlashCard",
                TestCaseID = "TC-VOCAB-FC-03",
                Description = "Topic có 2 vocab nhưng chỉ 1 Active → chỉ trả về 1 flashcard",
                ExpectedResult = "Return 200, Data.Count = 1, chỉ vocab Active",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "1 VocabularyTopic Active, 1 Deleted",
                    "Filter by Status = Active",
                    "Return 200, Count = 1"
                }
            });
        }
    }
}