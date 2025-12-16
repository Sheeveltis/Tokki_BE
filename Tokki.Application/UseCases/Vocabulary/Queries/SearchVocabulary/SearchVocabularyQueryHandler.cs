// SearchVocabularyQueryHandler.cs
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Queries.SearchVocabulary
{
    public class SearchVocabularyQueryHandler
        : IRequestHandler<SearchVocabularyQuery, OperationResult<PagedResult<VocabularySearchResultDto>>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IMemoryCache _cache;

        // Cache configuration
        private const int CACHE_DURATION_MINUTES = 5;
        private const string CACHE_KEY_PREFIX = "vocab_search";

        public SearchVocabularyQueryHandler(
            IVocabularyRepository vocabularyRepository,
            IMemoryCache cache)
        {
            _vocabularyRepository = vocabularyRepository;
            _cache = cache;
        }

        public async Task<OperationResult<PagedResult<VocabularySearchResultDto>>> Handle(
            SearchVocabularyQuery request,
            CancellationToken cancellationToken)
        {
            // ===== VALIDATION =====
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return OperationResult<PagedResult<VocabularySearchResultDto>>.Failure(
                    new List<Error>
                    {
                        new Error("INVALID_SEARCH_TERM", "Từ khóa tìm kiếm không được để trống.")
                    },
                    400
                );
            }

            if (request.SearchTerm.Length > 50)
            {
                return OperationResult<PagedResult<VocabularySearchResultDto>>.Failure(
                    new List<Error>
                    {
                        new Error("SEARCH_TERM_TOO_LONG", "Từ khóa tìm kiếm quá dài (tối đa 50 ký tự).")
                    },
                    400
                );
            }

            if (request.PageSize > 50)
            {
                request.PageSize = 50;
            }

            // ===== TẠO CACHE KEY =====
            var cacheKey =
                $"{CACHE_KEY_PREFIX}:{request.SearchTerm.ToLower().Trim()}:{request.PageNumber}:{request.PageSize}";

            // ===== CACHE HIT =====
            if (_cache.TryGetValue(cacheKey, out PagedResult<VocabularySearchResultDto> cachedResult))
            {
                return OperationResult<PagedResult<VocabularySearchResultDto>>.Success(
                    cachedResult,
                    200,
                    $"Tìm thấy {cachedResult.TotalCount} kết quả."
                );
            }

            // ===== QUERY DATABASE =====
            var (vocabularies, totalCount) =
                await _vocabularyRepository.SearchVocabulariesAsync(
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize
                );

            // Map sang DTO
            var results = vocabularies.Select(v => new VocabularySearchResultDto
            {
                VocabularyId = v.VocabularyId,
                Text = v.Text,
                Definition = v.Definition,
                Pronunciation = v.Pronunciation
            }).ToList();

            // Tạo paged result
            var pagedResult = PagedResult<VocabularySearchResultDto>.Create(
                results,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            // ===== SAVE CACHE =====
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES),
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, pagedResult, cacheEntryOptions);

            // ===== RETURN RESULT =====
            var message = totalCount > 0
                ? $"Tìm thấy {totalCount} kết quả."
                : "Không tìm thấy kết quả nào.";

            return OperationResult<PagedResult<VocabularySearchResultDto>>.Success(
                pagedResult,
                200,
                message
            );
        }
    }
}
