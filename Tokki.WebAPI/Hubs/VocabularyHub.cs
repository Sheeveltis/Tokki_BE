using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries.SearchVocabulary;

namespace Tokki.WebAPI.Hubs
{
    public class VocabularyHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly IMemoryCache _cache;
        private readonly ILogger<VocabularyHub> _logger;

        // Semaphore để giới hạn concurrent requests (tối đa 20 requests cùng lúc)
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(20, 20);

        // Rate limit configuration
        private const int MAX_REQUESTS_PER_MINUTE = 30;
        private const int MIN_SEARCH_LENGTH = 1;
        private const int MAX_PAGE_SIZE = 50;

        public VocabularyHub(
            IMediator mediator,
            IMemoryCache cache,
            ILogger<VocabularyHub> logger)
        {
            _mediator = mediator;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Search vocabulary với rate limiting và validation
        /// </summary>
        public async Task<OperationResult<PagedResult<VocabularySearchResultDto>>> SearchVocabulary(
            string searchTerm,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var connectionId = Context.ConnectionId;

            try
            {
                // ===== VALIDATION =====
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    _logger.LogWarning("🚫 Empty search term from connection: {ConnectionId}", connectionId);

                    return OperationResult<PagedResult<VocabularySearchResultDto>>.Failure(
                        new List<Error>
                        {
                            new Error("INVALID_SEARCH_TERM", "Từ khóa tìm kiếm không được để trống.")
                        },
                        400
                    );
                }

                if (searchTerm.Trim().Length < MIN_SEARCH_LENGTH)
                {
                    _logger.LogWarning("🚫 Search term too short: '{SearchTerm}' from connection: {ConnectionId}", searchTerm, connectionId);

                    return OperationResult<PagedResult<VocabularySearchResultDto>>.Failure(
                        new List<Error>
                        {
                            new Error("SEARCH_TERM_TOO_SHORT", $"Từ khóa tìm kiếm phải có ít nhất {MIN_SEARCH_LENGTH} ký tự.")
                        },
                        400
                    );
                }

                // ===== RATE LIMITING PER CONNECTION =====
                var rateLimitKey = $"rate_limit_vocab_search:{connectionId}";

                // 🔥 DEBUG LOG 1: Kiểm tra cache key
                _logger.LogInformation("🔍 Checking rate limit for key: {RateLimitKey}", rateLimitKey);

                if (_cache.TryGetValue(rateLimitKey, out int requestCount))
                {
                    // 🔥 DEBUG LOG 2: Hiển thị số lượng requests hiện tại
                    _logger.LogWarning("📊 Rate limit check: {RequestCount}/{MaxRequests} for connection: {ConnectionId}",
                        requestCount,
                        MAX_REQUESTS_PER_MINUTE,
                        connectionId);

                    if (requestCount >= MAX_REQUESTS_PER_MINUTE)
                    {
                        // 🔥 DEBUG LOG 3: Rate limit exceeded
                        _logger.LogError(
                            "🚫 RATE LIMIT EXCEEDED! Connection: {ConnectionId}, Requests: {RequestCount}/{MaxRequests}",
                            connectionId,
                            requestCount,
                            MAX_REQUESTS_PER_MINUTE
                        );

                        return OperationResult<PagedResult<VocabularySearchResultDto>>.Failure(
                            new List<Error>
                            {
                                new Error(
                                    "RATE_LIMIT_EXCEEDED",
                                    $"Bạn đã vượt quá giới hạn {MAX_REQUESTS_PER_MINUTE} requests/phút. Vui lòng thử lại sau."
                                )
                            },
                            429 // Too Many Requests
                        );
                    }

                    // Tăng counter
                    _cache.Set(rateLimitKey, requestCount + 1, TimeSpan.FromMinutes(1));

                    // 🔥 DEBUG LOG 4: Counter đã tăng
                    _logger.LogInformation("✅ Incremented counter to {NewCount} for connection: {ConnectionId}",
                        requestCount + 1,
                        connectionId);
                }
                else
                {
                    // Tạo counter mới
                    _cache.Set(rateLimitKey, 1, TimeSpan.FromMinutes(1));

                    // 🔥 DEBUG LOG 5: Tạo counter mới
                    _logger.LogInformation("🆕 Created new rate limit counter for connection: {ConnectionId}", connectionId);
                }

                // ===== THROTTLE CONCURRENT REQUESTS =====
                // Chờ nếu đã có quá nhiều requests đang xử lý
                var hasAcquired = await _semaphore.WaitAsync(TimeSpan.FromSeconds(5));

                if (!hasAcquired)
                {
                    _logger.LogWarning(
                        "⏱️ Semaphore timeout for connection {ConnectionId}",
                        connectionId
                    );

                    return OperationResult<PagedResult<VocabularySearchResultDto>>.Failure(
                        new List<Error>
                        {
                            new Error("SERVER_BUSY", "Server đang xử lý quá nhiều requests. Vui lòng thử lại sau.")
                        },
                        503 // Service Unavailable
                    );
                }

                try
                {
                    // ===== EXECUTE QUERY =====
                    var query = new SearchVocabularyQuery
                    {
                        SearchTerm = searchTerm.Trim(),
                        PageNumber = pageNumber,
                        PageSize = Math.Min(pageSize, MAX_PAGE_SIZE) // Giới hạn max page size
                    };

                    _logger.LogInformation(
                        "🔍 Searching vocabulary: Term='{SearchTerm}', Page={PageNumber}, PageSize={PageSize}, Connection={ConnectionId}",
                        query.SearchTerm,
                        query.PageNumber,
                        query.PageSize,
                        connectionId
                    );

                    var result = await _mediator.Send(query);

                    // 🔥 DEBUG LOG 6: Query thành công
                    _logger.LogInformation("✅ Search completed: Found {TotalCount} results for term '{SearchTerm}'",
                        result.Data?.TotalCount ?? 0,
                        searchTerm);

                    return result;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error searching vocabulary: Term='{SearchTerm}', Connection={ConnectionId}",
                    searchTerm,
                    connectionId
                );

                return OperationResult<PagedResult<VocabularySearchResultDto>>.Failure(
                    new List<Error>
                    {
                        new Error("INTERNAL_ERROR", "Đã xảy ra lỗi khi tìm kiếm. Vui lòng thử lại sau.")
                    },
                    500
                );
            }
        }

        // ===== CONNECTION LIFECYCLE =====
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation(
                "✅ Client connected to VocabularyHub: {ConnectionId}",
                Context.ConnectionId
            );

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;

            // Cleanup rate limit cache khi disconnect
            var rateLimitKey = $"rate_limit_vocab_search:{connectionId}";
            _cache.Remove(rateLimitKey);

            _logger.LogInformation("🧹 Cleaned up rate limit cache for connection: {ConnectionId}", connectionId);

            if (exception != null)
            {
                _logger.LogWarning(
                    exception,
                    "⚠️ Client disconnected from VocabularyHub with error: {ConnectionId}",
                    connectionId
                );
            }
            else
            {
                _logger.LogInformation(
                    "👋 Client disconnected from VocabularyHub: {ConnectionId}",
                    connectionId
                );
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}