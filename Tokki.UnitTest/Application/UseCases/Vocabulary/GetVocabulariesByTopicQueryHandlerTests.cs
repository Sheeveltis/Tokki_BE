using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Queries.GetVocabulariesByTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class GetVocabulariesByTopicQueryHandlerTests
    {
        private GetVocabulariesByTopicQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new GetVocabulariesByTopicQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabularies_By_Topic_01 | A | Topic not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            // Arrange
            var query = new GetVocabulariesByTopicQuery { TopicId = "TOPIC-INVALID" };
            var handler = CreateHandler(topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Topic", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabularies By Topic",
                TestCaseID        = "Get_Vocabularies_By_Topic_01",
                Description       = "Get vocab by topic with non-existent TopicId",
                ExpectedResult    = "Return 404 TOPIC_NOT_FOUND",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Topic = null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabularies_By_Topic_02 | N | Topic exists, no vocabs → 200 empty paged
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopicExistsNoVocabs_ShouldReturn200EmptyPaged()
        {
            // Arrange
            var query = new GetVocabulariesByTopicQuery { TopicId = "TOPIC-001", PageNumber = 1, PageSize = 10 };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTopicAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<VocabularyStatus?>(), It.IsAny<string?>()))
                .ReturnsAsync((new List<Tokki.Domain.Entities.Vocabulary>(), 0));

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(0);
            result.Data.Items.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Topic", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabularies By Topic",
                TestCaseID        = "Get_Vocabularies_By_Topic_02",
                Description       = "Topic exists but has no vocabularies → returns 200 empty paged",
                ExpectedResult    = "Return 200, TotalCount = 0, Items = empty",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid TopicId", "No vocabs in topic", "Return 200 empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabularies_By_Topic_03 | N | Topic with vocabs → 200 paged list
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopicWithVocabs_ShouldReturn200PagedList()
        {
            // Arrange
            var query = new GetVocabulariesByTopicQuery { TopicId = "TOPIC-001", PageNumber = 1, PageSize = 10 };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active),
                MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-002")
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTopicAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<VocabularyStatus?>(), It.IsAny<string?>()))
                .ReturnsAsync((vocabs, 2));

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(2);
            result.Data.Items.Should().HaveCount(2);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Topic", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabularies By Topic",
                TestCaseID        = "Get_Vocabularies_By_Topic_03",
                Description       = "Topic exists with 2 vocabularies → return paged list 200",
                ExpectedResult    = "Return 200, TotalCount = 2, Items.Count = 2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid TopicId", "2 vocabs in topic", "Return 200 paged" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabularies_By_Topic_04 | N | Filter by Status = Active → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByStatusActive_ShouldReturn200()
        {
            // Arrange
            var query = new GetVocabulariesByTopicQuery
            {
                TopicId    = "TOPIC-001",
                Status     = VocabularyStatus.Active,
                PageNumber = 1,
                PageSize   = 10
            };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active)
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTopicAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    VocabularyStatus.Active, It.IsAny<string?>()))
                .ReturnsAsync((vocabs, 1));

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(1);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Topic", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabularies By Topic",
                TestCaseID        = "Get_Vocabularies_By_Topic_04",
                Description       = "Filter by Status = Active → returns only Active vocabs",
                ExpectedResult    = "Return 200, TotalCount = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status filter = Active", "1 matching vocab", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabularies_By_Topic_05 | N | SearchText filter → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterBySearchText_ShouldReturn200()
        {
            // Arrange
            var query = new GetVocabulariesByTopicQuery
            {
                TopicId    = "TOPIC-001",
                SearchText = "감사",
                PageNumber = 1,
                PageSize   = 10
            };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-002")
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTopicAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<VocabularyStatus?>(), "감사"))
                .ReturnsAsync((vocabs, 1));

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic()));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(1);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Topic", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabularies By Topic",
                TestCaseID        = "Get_Vocabularies_By_Topic_05",
                Description       = "Filter by SearchText '감사' → returns matching vocab in topic",
                ExpectedResult    = "Return 200, TotalCount = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchText filter set", "1 matching vocab", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabularies_By_Topic_06 | N | Vocab has topic mapping → DTO includes topic info
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabWithTopicMappings_ShouldReturnDtoWithTopics()
        {
            // Arrange
            var query = new GetVocabulariesByTopicQuery { TopicId = "TOPIC-001", PageNumber = 1, PageSize = 10 };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTopicAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<VocabularyStatus?>(), It.IsAny<string?>()))
                .ReturnsAsync((new List<Tokki.Domain.Entities.Vocabulary> { vocab }, 1));

            var vocabTopicMappings = MockVocabularyTopicRepository.GetSampleActiveTopicMappings("VOCAB-001", "TOPIC-001");
            var vocabTopicRepo = MockVocabularyTopicRepository.GetMock(returnedByVocabId: vocabTopicMappings);
            var topicRepo = MockTopicRepository.GetMock(returnedTopic: MockTopicRepository.GetSampleTopic());

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                topicRepo: topicRepo,
                vocabTopicRepo: vocabTopicRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].Topics.Should().NotBeEmpty();
            result.Data.Items[0].Topics[0].TopicId.Should().Be("TOPIC-001");

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Topic", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabularies By Topic",
                TestCaseID        = "Get_Vocabularies_By_Topic_06",
                Description       = "Vocab has topic mapping → DTO.Topics populated with correct topic info",
                ExpectedResult    = "Return 200, DTO.Topics.Count > 0, TopicId = TOPIC-001",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab has active topic mapping", "Topic info enriched in DTO", "Return 200" }
            });
        }
    }
}
