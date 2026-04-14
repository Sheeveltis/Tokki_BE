using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-FC-01 | A | Topic not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            // Arrange
            var query = new FlashCardQuery { TopicId = "TOPIC-INVALID" };
            var handler = CreateHandler(topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Flash Card", new TestCaseDetail
            {
                FunctionGroup     = "Get Flash Card",
                TestCaseID        = "TC-VOCAB-FC-01",
                Description       = "Get flashcards with non-existent TopicId",
                ExpectedResult    = "Return 404 TopicNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Topic = null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-FC-02 | N | Topic exists, no VocabTopic mappings → 200, empty list
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopicHasNoVocabMappings_ShouldReturn200EmptyList()
        {
            // Arrange
            var query = new FlashCardQuery { TopicId = "TOPIC-001" };
            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(
                    returnedByTopicId: new List<VocabularyTopic>()));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Flash Card", new TestCaseDetail
            {
                FunctionGroup     = "Get Flash Card",
                TestCaseID        = "TC-VOCAB-FC-02",
                Description       = "Topic exists but has no VocabularyTopic relationships → empty list",
                ExpectedResult    = "Return 200, Data = empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid TopicId", "No VocabularyTopic relationships", "Return 200 empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-FC-03 | N | Only Active VocabTopics returned, Deleted filtered
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MixedStatusVocabTopics_ShouldReturnOnlyActiveVocabs()
        {
            // Arrange
            var query = new FlashCardQuery { TopicId = "TOPIC-001" };
            var vocabTopics = new List<VocabularyTopic>
            {
                new VocabularyTopic { VocabularyId = "VOCAB-001", TopicId = "TOPIC-001", Status = VocabularyTopicStatus.Active },
                new VocabularyTopic { VocabularyId = "VOCAB-002", TopicId = "TOPIC-001", Status = VocabularyTopicStatus.Deleted }
            };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active)
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(vocabs);

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(returnedByTopicId: vocabTopics));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(1);
            result.Data[0].VocabularyId.Should().Be("VOCAB-001");

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Flash Card", new TestCaseDetail
            {
                FunctionGroup     = "Get Flash Card",
                TestCaseID        = "TC-VOCAB-FC-03",
                Description       = "Topic has 2 VocabTopics (1 Active, 1 Deleted) → only Active returned",
                ExpectedResult    = "Return 200, Data.Count = 1, only Active vocab",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 Active VocabTopic, 1 Deleted", "Filter by Status = Active", "Return 200, Count = 1" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-FC-04 | N | All vocabs Active → return full list → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AllActiveVocabs_ShouldReturnFullList()
        {
            // Arrange
            var query = new FlashCardQuery { TopicId = "TOPIC-001" };
            var vocabTopics = new List<VocabularyTopic>
            {
                new VocabularyTopic { VocabularyId = "VOCAB-001", TopicId = "TOPIC-001", Status = VocabularyTopicStatus.Active },
                new VocabularyTopic { VocabularyId = "VOCAB-002", TopicId = "TOPIC-001", Status = VocabularyTopicStatus.Active }
            };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active),
                MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-002")
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(vocabs);

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(returnedByTopicId: vocabTopics));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(1); // Only Active vocab passes vocab.Status filter

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Flash Card", new TestCaseDetail
            {
                FunctionGroup     = "Get Flash Card",
                TestCaseID        = "TC-VOCAB-FC-04",
                Description       = "2 Active VocabTopics, but one vocab has Status=PendingApproval → only Active vocab passes",
                ExpectedResult    = "Return 200, Data.Count = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 Active VocabTopics", "1 vocab.Status != Active filtered out", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-FC-05 | A | VocabTopics only Deleted → return empty → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AllDeletedVocabTopics_ShouldReturn200WithEmpty()
        {
            // Arrange
            var query = new FlashCardQuery { TopicId = "TOPIC-001" };
            var vocabTopics = new List<VocabularyTopic>
            {
                new VocabularyTopic { VocabularyId = "VOCAB-004", TopicId = "TOPIC-001", Status = VocabularyTopicStatus.Deleted }
            };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(returnedByTopicId: vocabTopics));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Flash Card", new TestCaseDetail
            {
                FunctionGroup     = "Get Flash Card",
                TestCaseID        = "TC-VOCAB-FC-05",
                Description       = "Topic has VocabTopics but all are Deleted → no Active vocabs → empty list",
                ExpectedResult    = "Return 200, Data = empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All VocabTopics = Deleted", "Active filter yields 0", "Return 200 empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-FC-06 | N | Valid flashcard with audio and image → 200 with full DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabWithAudioAndImage_ShouldReturnDtoWithFields()
        {
            // Arrange
            var query = new FlashCardQuery { TopicId = "TOPIC-001" };
            var vocabTopics = new List<VocabularyTopic>
            {
                new VocabularyTopic { VocabularyId = "VOCAB-001", TopicId = "TOPIC-001", Status = VocabularyTopicStatus.Active }
            };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);
            vocab.ImgURL   = "https://cdn.tokki.com/img/vocab.jpg";
            vocab.AudioURL = "https://cdn.tokki.com/audio/vocab.mp3";

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary> { vocab });

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(returnedByTopicId: vocabTopics));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(1);
            result.Data[0].AudioUrl.Should().Be("https://cdn.tokki.com/audio/vocab.mp3");
            result.Data[0].ImgURL.Should().Be("https://cdn.tokki.com/img/vocab.jpg");

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Flash Card", new TestCaseDetail
            {
                FunctionGroup     = "Get Flash Card",
                TestCaseID        = "TC-VOCAB-FC-06",
                Description       = "Valid flashcard retrieval – vocab has AudioURL and ImgURL → DTO fields mapped correctly",
                ExpectedResult    = "Return 200, AudioUrl and ImgURL populated",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab.AudioURL set", "Vocab.ImgURL set", "Return 200 with DTO" }
            });
        }
    }
}