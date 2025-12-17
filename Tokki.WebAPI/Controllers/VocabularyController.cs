using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies;
using Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary;
using Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries;
using Tokki.Application.UseCases.Vocabulary.Queries.FlashCard;
using Tokki.Application.UseCases.Vocabulary.Queries.GetVocabulariesByTopic;
using Tokki.Application.UseCases.Vocabulary.Queries.SearchVocabulary;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class VocabularyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VocabularyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ==========================================
        // EXISTING ENDPOINTS (GIỮ NGUYÊN)
        // ==========================================

        /// <summary>
        /// Tạo hàng loạt vocabulary
        /// </summary>
        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> BulkCreateVocabularies([FromBody] BulkCreateVocabulariesCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Search vocabulary for user (OLD - giữ để backward compatibility)
        /// </summary>
        [HttpGet("search-for-user")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchVocabulary(
           [FromQuery] string searchTerm,
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 20)
        {
            var query = new SearchVocabularyQuery
            {
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get flash card by topic
        /// </summary>
        [HttpGet("flash-card")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFlashCardByTopic([FromQuery] FlashCardQuery command)
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật vocabulary
        /// </summary>
        [HttpPut("{vocabularyId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateVocabulary(
            string vocabularyId,
            [FromBody] VocabularyUpdateDto updateData)
        {
            var command = new UpdateVocabularyCommand
            {
                VocabularyId = vocabularyId,
                UpdateData = updateData
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Xóa vocabulary (soft delete)
        /// </summary>
        [HttpDelete("{vocabularyId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteVocabulary(string vocabularyId)
        {
            var command = new DeleteVocabularyCommand
            {
                VocabularyId = vocabularyId
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả nghĩa của một từ
        /// </summary>
        [HttpGet("by-text")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVocabularyByText(
            [FromQuery] string text,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? topicId = null,
            [FromQuery] VocabularyStatus? status = null)
        {
            var query = new GetVocabularyByTextQuery
            {
                Text = text,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TopicId = topicId,
                Status = status
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy vocabularies theo topic
        /// </summary>
        [HttpGet("by-topic/{topicId}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVocabulariesByTopic(
            string topicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] VocabularyStatus? status = null,
            [FromQuery] string? searchText = null)
        {
            var query = new GetVocabulariesByTopicQuery
            {
                TopicId = topicId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                SearchText = searchText
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Search vocabularies (tìm kiếm từ vựng)
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchVocabularies(
            [FromQuery] string? searchText = null,
            [FromQuery] string? topicId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] VocabularyStatus? status = null)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return BadRequest(new { message = "Vui lòng nhập từ khóa tìm kiếm" });
            }

            var query = new GetVocabulariesByTopicQuery
            {
                TopicId = topicId ?? string.Empty,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                SearchText = searchText
            };

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        // ==========================================
        // 🔥 SIGNALR TESTING ENDPOINTS (ĐÁNH DẤU ĐẶC BIỆT)
        // ==========================================

        /// <summary>
        /// 🔥 [SIGNALR-TEST] Search vocabulary - Optimized for realtime (dùng chung handler với SignalR Hub)
        /// </summary>
        /// <remarks>
        /// **Endpoint này dùng để:**
        /// - Test SignalR Hub logic trên Swagger
        /// - Share cùng business logic với VocabularyHub
        /// - Test cache performance, rate limiting
        /// 
        /// **Khác với `/search`:**
        /// - `/search`: General search với complex filters
        /// - `/signalr-test/search`: Optimized cho realtime, có cache
        /// </remarks>
        /// <param name="searchTerm">Từ khóa tìm kiếm (VD: 은행, 학교)</param>
        /// <param name="pageNumber">Trang số (mặc định: 1)</param>
        /// <param name="pageSize">Số lượng kết quả mỗi trang (mặc định: 20, tối đa: 50)</param>
        [HttpGet("signalr-test/search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OperationResult<PagedResult<VocabularySearchResultDto>>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SignalRTestSearch(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // ===== GỌI CÙNG HANDLER VỚI SIGNALR HUB =====
            var query = new SearchVocabularyQuery
            {
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            // ===== TRẢ VỀ HTTP STATUS CODE TƯƠNG ỨNG =====
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// 🔥 [SIGNALR-TEST] Get vocabulary by ID
        /// </summary>
        [HttpGet("signalr-test/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VocabularySearchResultDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SignalRTestGetById(int id)
        {
            // TODO: Implement GetVocabularyByIdQuery nếu cần
            return Ok(new { message = "Coming soon...", id });
        }

        /// <summary>
        /// 🔥 [SIGNALR-TEST] Test cache performance - So sánh tốc độ DB vs Cache
        /// </summary>
        /// <remarks>
        /// **Mục đích:**
        /// - Test xem cache có hoạt động không
        /// - Đo performance improvement
        /// - Verify cache hit/miss
        /// 
        /// **Cách test:**
        /// 1. Gọi endpoint với searchTerm="은행"
        /// 2. Xem firstCallMs (query DB) vs secondCallMs (from cache)
        /// 3. Cache nhanh hơn 10-30x là bình thường
        /// </remarks>
        [HttpGet("signalr-test/cache-performance")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> SignalRTestCachePerformance(
            [FromQuery] string searchTerm = "은행")
        {
            var sw = Stopwatch.StartNew();

            // First call (from DB)
            var query1 = new SearchVocabularyQuery
            {
                SearchTerm = searchTerm,
                PageNumber = 1,
                PageSize = 20
            };
            await _mediator.Send(query1);
            sw.Stop();
            var firstCallMs = sw.ElapsedMilliseconds;

            // Second call (from cache)
            sw.Restart();
            var query2 = new SearchVocabularyQuery
            {
                SearchTerm = searchTerm,
                PageNumber = 1,
                PageSize = 20
            };
            var result = await _mediator.Send(query2);
            sw.Stop();
            var secondCallMs = sw.ElapsedMilliseconds;

            return Ok(new
            {
                testType = "Cache Performance Test",
                searchTerm,
                firstCallMs,
                secondCallMs,
                speedImprovement = firstCallMs > 0
                    ? $"{firstCallMs / (double)Math.Max(secondCallMs, 1):F2}x faster"
                    : "N/A",
                interpretation = secondCallMs < 5
                    ? "✅ Cache is working perfectly!"
                    : "⚠️ Cache might not be working",
                result
            });
        }

        /// <summary>
        /// 🔥 [SIGNALR-TEST] Clear vocabulary search cache
        /// </summary>
        /// <remarks>
        /// **Chức năng:**
        /// - Xóa toàn bộ cache của vocabulary search
        /// - Dùng khi cần test cache từ đầu
        /// 
        /// **Lưu ý:** 
        /// - Feature này chưa implement
        /// - Cần inject IMemoryCache và xóa cache keys
        /// </remarks>
        [HttpDelete("signalr-test/cache")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        public IActionResult SignalRTestClearCache()
        {
            // TODO: Implement cache clearing
            // Cần inject IMemoryCache vào constructor
            // Và xóa các keys có prefix "vocab_search:"
            return Ok(new
            {
                message = "⚠️ Cache clearing not implemented yet",
                howToImplement = "Inject IMemoryCache and remove keys with prefix 'vocab_search:'"
            });
        }

        // ==========================================
        // 🧪 LEGACY TEST ENDPOINT (Có thể xóa sau)
        // ==========================================

        /// <summary>
        /// 🧪 [DEPRECATED] Test cache - Dùng signalr-test/cache-performance thay thế
        /// </summary>
        [HttpGet("test-cache")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)] // Ẩn khỏi Swagger UI
        public async Task<IActionResult> TestCache()
        {
            var sw = Stopwatch.StartNew();

            // Lần 1: Query DB
            var result1 = await _mediator.Send(new SearchVocabularyQuery
            {
                SearchTerm = "운",
                PageNumber = 1,
                PageSize = 20
            });
            sw.Stop();
            var time1 = sw.ElapsedMilliseconds;

            sw.Restart();

            // Lần 2: Get from cache
            var result2 = await _mediator.Send(new SearchVocabularyQuery
            {
                SearchTerm = "은행",
                PageNumber = 1,
                PageSize = 20
            });
            sw.Stop();
            var time2 = sw.ElapsedMilliseconds;

            return Ok(new
            {
                deprecationNotice = "⚠️ Use /signalr-test/cache-performance instead",
                FirstQuery = $"{time1}ms (DB)",
                SecondQuery = $"{time2}ms (Cache)",
                Improvement = $"{(time1 - time2)}ms faster"
            });
        }
    }
}