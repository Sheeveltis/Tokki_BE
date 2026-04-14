using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries.SearchVocabulary;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class SearchVocabularyQueryHandlerTests
    {
        private SearchVocabularyQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            IMemoryCache? cache = null)
        {
            return new SearchVocabularyQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                cache ?? new MemoryCache(new MemoryCacheOptions()));
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-SRC-01 | A | Empty SearchTerm → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptySearchTerm_ShouldReturn400()
        {
            // Arrange
            var query = new SearchVocabularyQuery { SearchTerm = "" };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup     = "Search Vocabulary",
                TestCaseID        = "TC-VOCAB-SRC-01",
                Description       = "Search with empty SearchTerm",
                ExpectedResult    = "Return 400 INVALID_SEARCH_TERM",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchTerm = empty string", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-SRC-02 | B | SearchTerm > 50 chars → 400 (boundary)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SearchTermTooLong_ShouldReturn400()
        {
            // Arrange
            var query = new SearchVocabularyQuery { SearchTerm = new string('A', 51) };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup     = "Search Vocabulary",
                TestCaseID        = "TC-VOCAB-SRC-02",
                Description       = "Search with SearchTerm 51 chars long (exceeds limit of 50)",
                ExpectedResult    = "Return 400 SEARCH_TERM_TOO_LONG",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchTerm.Length = 51 (max+1)", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-SRC-03 | B | SearchTerm exactly 50 chars → passes validation → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SearchTermExactly50Chars_ShouldPassValidation()
        {
            // Arrange
            var query = new SearchVocabularyQuery { SearchTerm = new string('A', 50) };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.SearchVocabulariesAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult((
                    Items: new List<VocabularySearchResultDto>(),
                    TotalCount: 0)));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup     = "Search Vocabulary",
                TestCaseID        = "TC-VOCAB-SRC-03",
                Description       = "SearchTerm is exactly 50 characters (boundary: max) → validation passes",
                ExpectedResult    = "Return 200, validation not blocked",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SearchTerm.Length = 50 (boundary: at limit)", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-SRC-04 | N | Valid search → returns results → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSearch_ShouldReturnResults200()
        {
            // Arrange
            var query = new SearchVocabularyQuery { SearchTerm = "안녕", PageNumber = 1, PageSize = 10 };
            var vocabs = new List<VocabularySearchResultDto>
            {
                new VocabularySearchResultDto { VocabularyId = "VOCAB-001", Text = "안녕" },
                new VocabularySearchResultDto { VocabularyId = "VOCAB-002", Text = "안녕하세요" }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.SearchVocabulariesAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult((Items: vocabs, TotalCount: 2)));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(2);
            result.Data.Items.Should().HaveCount(2);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup     = "Search Vocabulary",
                TestCaseID        = "TC-VOCAB-SRC-04",
                Description       = "Valid search term → returns 2 vocab results → 200",
                ExpectedResult    = "Return 200, TotalCount = 2, Items.Count = 2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid SearchTerm", "2 vocab matches", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-SRC-05 | N | Cache hit on second search call → DB called once
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SecondCallSameKey_ShouldUseCacheAndCallDbOnce()
        {
            // Arrange
            var query = new SearchVocabularyQuery { SearchTerm = "단어", PageNumber = 1, PageSize = 10 };
            var vocabs = new List<VocabularySearchResultDto>
            {
                new VocabularySearchResultDto { VocabularyId = "VOCAB-001", Text = "단어" }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.SearchVocabulariesAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult((Items: vocabs, TotalCount: 1)));

            var realCache = new MemoryCache(new MemoryCacheOptions());
            var handler = CreateHandler(vocabRepo: mockVocabRepo, cache: realCache);

            // Act
            var result1 = await handler.Handle(query, CancellationToken.None);
            var result2 = await handler.Handle(query, CancellationToken.None);

            // Assert
            result1.IsSuccess.Should().BeTrue();
            result2.IsSuccess.Should().BeTrue();
            result2.Data.TotalCount.Should().Be(1);
            mockVocabRepo.Verify(x => x.SearchVocabulariesAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup     = "Search Vocabulary",
                TestCaseID        = "TC-VOCAB-SRC-05",
                Description       = "Same search term called twice → 2nd call uses cache, DB called only once",
                ExpectedResult    = "Return 200 twice, DB called exactly 1 time",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Same search term", "Cache hit on 2nd call", "DB only once" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-SRC-06 | N | Search with zero results → 200 empty list
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoResults_ShouldReturn200WithEmptyList()
        {
            // Arrange
            var query = new SearchVocabularyQuery { SearchTerm = "없는단어" };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.SearchVocabulariesAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult((
                    Items: new List<VocabularySearchResultDto>(),
                    TotalCount: 0)));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(0);
            result.Data.Items.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup     = "Search Vocabulary",
                TestCaseID        = "TC-VOCAB-SRC-06",
                Description       = "Valid search term but no matching results → 200 empty list",
                ExpectedResult    = "Return 200, TotalCount = 0, Items = empty",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid SearchTerm", "No matching vocab", "Return 200 empty" }
            });
        }
    }
}