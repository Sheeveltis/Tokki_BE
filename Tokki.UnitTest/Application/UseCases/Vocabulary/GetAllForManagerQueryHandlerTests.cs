using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries.GetAllForManager;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class GetAllForManagerQueryHandlerTests
    {
        private GetAllForManagerQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new GetAllForManagerQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GAM-01 | N | No filter → returns all paged → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoFilter_ShouldReturnPagedList200()
        {
            // Arrange
            var query = new GetAllForManagerQuery { PageNumber = 1, PageSize = 20 };

            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active),
                MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-002")
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesForManagerAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<VocabularyStatus?>(), It.IsAny<string?>(), It.IsAny<TopicLevel?>()))
                .ReturnsAsync((vocabs, 2));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(2);
            result.Data.Items.Should().HaveCount(2);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get All For Manager", new TestCaseDetail
            {
                FunctionGroup     = "Get All Vocabularies For Manager",
                TestCaseID        = "TC-VOCAB-GAM-01",
                Description       = "Get all manager vocabs without any filter → returns all paged",
                ExpectedResult    = "Return 200, TotalCount = 2, Items.Count = 2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No filter", "2 vocabs", "Return 200 paged" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GAM-02 | N | Filter by Status = PendingApproval → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByStatusPendingApproval_ShouldReturn200()
        {
            // Arrange
            var query = new GetAllForManagerQuery
            {
                PageNumber = 1,
                PageSize = 10,
                Status = VocabularyStatus.PendingApproval
            };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-002")
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesForManagerAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    VocabularyStatus.PendingApproval, It.IsAny<string?>(), It.IsAny<TopicLevel?>()))
                .ReturnsAsync((vocabs, 1));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(1);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get All For Manager", new TestCaseDetail
            {
                FunctionGroup     = "Get All Vocabularies For Manager",
                TestCaseID        = "TC-VOCAB-GAM-02",
                Description       = "Filter by Status = PendingApproval → returns only pending vocabs",
                ExpectedResult    = "Return 200, TotalCount = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status filter = PendingApproval", "1 matching vocab", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GAM-03 | N | Filter by SearchText → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterBySearchText_ShouldReturn200()
        {
            // Arrange
            var query = new GetAllForManagerQuery { SearchText = "안녕" };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active)
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesForManagerAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<VocabularyStatus?>(), "안녕", It.IsAny<TopicLevel?>()))
                .ReturnsAsync((vocabs, 1));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Items.Should().HaveCount(1);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get All For Manager", new TestCaseDetail
            {
                FunctionGroup     = "Get All Vocabularies For Manager",
                TestCaseID        = "TC-VOCAB-GAM-03",
                Description       = "Filter by SearchText '안녕' → returns matching vocab",
                ExpectedResult    = "Return 200, Items.Count = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchText = '안녕'", "1 matching vocab", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GAM-04 | N | No vocabs match → 200 empty paged
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoVocabsExist_ShouldReturn200EmptyPaged()
        {
            // Arrange
            var query = new GetAllForManagerQuery { PageNumber = 1, PageSize = 10 };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesForManagerAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<VocabularyStatus?>(), It.IsAny<string?>(), It.IsAny<TopicLevel?>()))
                .ReturnsAsync((new List<Tokki.Domain.Entities.Vocabulary>(), 0));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(0);
            result.Data.Items.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get All For Manager", new TestCaseDetail
            {
                FunctionGroup     = "Get All Vocabularies For Manager",
                TestCaseID        = "TC-VOCAB-GAM-04",
                Description       = "No vocabs match filter → returns empty paged result",
                ExpectedResult    = "Return 200, TotalCount = 0, Items = empty",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No matching vocab", "Return 200 empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GAM-05 | N | Vocab with active topic mapping → LevelTopic set
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabWithActiveTopic_ShouldEnrichLevelTopic()
        {
            // Arrange
            var query = new GetAllForManagerQuery { PageNumber = 1, PageSize = 10 };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesForManagerAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<VocabularyStatus?>(), It.IsAny<string?>(), It.IsAny<TopicLevel?>()))
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
            result.Data.Items[0].LevelTopic.Should().NotBeNull();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get All For Manager", new TestCaseDetail
            {
                FunctionGroup     = "Get All Vocabularies For Manager",
                TestCaseID        = "TC-VOCAB-GAM-05",
                Description       = "Vocab has active topic mapping → LevelTopic enriched in DTO",
                ExpectedResult    = "Return 200, Items[0].LevelTopic != null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab has 1 active topic mapping", "LevelTopic enriched", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GAM-06 | N | Filter by LevelTopic → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByLevelTopic_ShouldReturn200()
        {
            // Arrange
            var query = new GetAllForManagerQuery { LevelTopic = TopicLevel.Level1 };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active)
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesForManagerAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<VocabularyStatus?>(), It.IsAny<string?>(), TopicLevel.Level1))
                .ReturnsAsync((vocabs, 1));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(1);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get All For Manager", new TestCaseDetail
            {
                FunctionGroup     = "Get All Vocabularies For Manager",
                TestCaseID        = "TC-VOCAB-GAM-06",
                Description       = "Filter by LevelTopic = Level1 → returns matching vocab",
                ExpectedResult    = "Return 200, TotalCount = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LevelTopic filter = Level1", "1 matching vocab", "Return 200" }
            });
        }
    }
}
