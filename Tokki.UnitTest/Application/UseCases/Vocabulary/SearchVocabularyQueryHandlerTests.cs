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

        [Fact]
        public async Task Handle_EmptySearchTerm_ShouldReturn400()
        {
            var query = new SearchVocabularyQuery { SearchTerm = "" };

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup = "Search Vocabulary",
                TestCaseID = "TC-VOCAB-SRC-01",
                Description = "Tìm kiếm với SearchTerm rỗng",
                ExpectedResult = "Return 400 INVALID_SEARCH_TERM",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "SearchTerm = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_SearchTermTooLong_ShouldReturn400()
        {
            // SearchTerm dài hơn 50 ký tự → 400
            var query = new SearchVocabularyQuery
            {
                SearchTerm = new string('A', 51) // 51 chars (boundary + 1)
            };

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup = "Search Vocabulary",
                TestCaseID = "TC-VOCAB-SRC-02",
                Description = "Tìm kiếm với SearchTerm dài 51 ký tự (vượt giới hạn 50)",
                ExpectedResult = "Return 400 SEARCH_TERM_TOO_LONG",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "SearchTerm.Length = 51 (boundary: max + 1)",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_SearchTermExactly50Chars_ShouldPassValidation()
        {
            // SearchTerm đúng 50 ký tự → hợp lệ
            var query = new SearchVocabularyQuery
            {
                SearchTerm = new string('A', 50) // exactly 50 chars
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.SearchVocabulariesAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(Task.FromResult((
                    Items: new List<Tokki.Application.UseCases.Vocabulary.DTOs.VocabularySearchResultDto>(),
                    TotalCount: 0
                )));
            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup = "Search Vocabulary",
                TestCaseID = "TC-VOCAB-SRC-03",
                Description = "SearchTerm đúng 50 ký tự (boundary: max) → validation pass",
                ExpectedResult = "Return 200, validation không bị block",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "SearchTerm.Length = 50 (boundary: exactly at limit)",
                    "No validation error",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidSearch_ShouldReturnResultsAndCacheHit()
        {
            var query = new SearchVocabularyQuery
            {
                SearchTerm = "안녕",
                PageNumber = 1,
                PageSize = 10
            };

            var vocabs = new List<VocabularySearchResultDto>
    {
        new VocabularySearchResultDto
        {
            VocabularyId = "VOCAB-001",
            Text = "안녕"
        },
        new VocabularySearchResultDto
        {
            VocabularyId = "VOCAB-002",
            Text = "안녕하세요"
        }
    };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.SearchVocabulariesAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(Task.FromResult((
                    Items: vocabs,
                    TotalCount: 2
                )));

            var realCache = new MemoryCache(new MemoryCacheOptions());
            var handler = CreateHandler(vocabRepo: mockVocabRepo, cache: realCache);

            // First call → hits DB
            var result1 = await handler.Handle(query, CancellationToken.None);

            // Second call → hits cache
            var result2 = await handler.Handle(query, CancellationToken.None);

            result1.IsSuccess.Should().BeTrue();
            result1.Data.TotalCount.Should().Be(2);
            result2.IsSuccess.Should().BeTrue();
            result2.Data.TotalCount.Should().Be(2);

            mockVocabRepo.Verify(x => x.SearchVocabulariesAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Once);

            QACollector.LogTestCase("Vocabulary - Search", new TestCaseDetail
            {
                FunctionGroup = "Search Vocabulary",
                TestCaseID = "TC-VOCAB-SRC-04",
                Description = "Tìm kiếm hợp lệ → lần 1 query DB, lần 2 dùng cache (DB không gọi lần 2)",
                ExpectedResult = "Return 200, TotalCount = 2, DB chỉ gọi 1 lần",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
        {
            "Valid SearchTerm",
            "2 vocab results",
            "Cache hit on second call",
            "DB called exactly once"
        }
            });
        }
    }
}