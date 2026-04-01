using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class GetVocabularyByTextQueryHandlerTests
    {
        private GetVocabularyByTextQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new GetVocabularyByTextQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GBT-01 | A | Empty Text → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyText_ShouldReturn400()
        {
            // Arrange
            var query = new GetVocabularyByTextQuery { Text = "" };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary By Text",
                TestCaseID        = "TC-VOCAB-GBT-01",
                Description       = "Get vocab by text with empty Text field",
                ExpectedResult    = "Return 400 INVALID_INPUT",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Text = empty string", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GBT-02 | A | Whitespace-only Text → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WhitespaceText_ShouldReturn400()
        {
            // Arrange
            var query = new GetVocabularyByTextQuery { Text = "   " };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary By Text",
                TestCaseID        = "TC-VOCAB-GBT-02",
                Description       = "Get vocab by text with whitespace-only Text",
                ExpectedResult    = "Return 400 INVALID_INPUT",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Text = whitespace only", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GBT-03 | A | No vocab matches text → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoVocabMatchesText_ShouldReturn404()
        {
            // Arrange
            var query = new GetVocabularyByTextQuery { Text = "없는단어" };
            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTextAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<string?>(), It.IsAny<VocabularyStatus?>()))
                .ReturnsAsync((new List<Tokki.Domain.Entities.Vocabulary>(), 0));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary By Text",
                TestCaseID        = "TC-VOCAB-GBT-03",
                Description       = "Get vocab by text with no matching vocabulary → 404",
                ExpectedResult    = "Return 404 VOCABULARY_NOT_FOUND",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TotalCount = 0", "No vocabulary matches text", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GBT-04 | N | Valid text with results → 200 paged list
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidText_ShouldReturnPagedList200()
        {
            // Arrange
            var query = new GetVocabularyByTextQuery { Text = "은행", PageNumber = 1, PageSize = 10 };

            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active),
                MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-002")
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTextAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<string?>(), It.IsAny<VocabularyStatus?>()))
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
            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary By Text",
                TestCaseID        = "TC-VOCAB-GBT-04",
                Description       = "Get vocab by text '은행' → finds 2 meanings → return paged 200",
                ExpectedResult    = "Return 200, TotalCount = 2, Items.Count = 2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid text", "2 matching vocabs", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GBT-05 | N | Filter by Status → only matching status returned
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByStatus_ShouldReturnFilteredResults()
        {
            // Arrange
            var query = new GetVocabularyByTextQuery
            {
                Text = "안녕",
                Status = VocabularyStatus.Active
            };
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active)
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTextAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<string?>(), VocabularyStatus.Active))
                .ReturnsAsync((vocabs, 1));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(1);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary By Text",
                TestCaseID        = "TC-VOCAB-GBT-05",
                Description       = "Get vocab by text with Status filter = Active → returns only Active vocab",
                ExpectedResult    = "Return 200, TotalCount = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Text valid", "Status filter = Active", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GBT-06 | N | Vocab has topic mappings → DTO includes topic info
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabWithTopicMappings_ShouldReturnDtoWithTopics()
        {
            // Arrange
            var query = new GetVocabularyByTextQuery { Text = "안녕하세요" };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTextAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<string?>(), It.IsAny<VocabularyStatus?>()))
                .ReturnsAsync((new List<Tokki.Domain.Entities.Vocabulary> { vocab }, 1));

            var vocabTopicRepo = MockVocabularyTopicRepository.GetMock(
                returnedByVocabId: MockVocabularyTopicRepository.GetSampleActiveTopicMappings());

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
            result.Data.Items[0].Topics.Should().NotBeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary By Text",
                TestCaseID        = "TC-VOCAB-GBT-06",
                Description       = "Vocab has topic mapping → DTO.Topics populated with topic info",
                ExpectedResult    = "Return 200, DTO.Topics.Count > 0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab has active topic mapping", "Topic info enriched", "Return 200" }
            });
        }
    }
}