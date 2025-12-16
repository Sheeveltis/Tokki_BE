using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies;
using Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary;
using Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries;
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

        // Test Controller hoặc Unit Test
        [HttpGet("test-cache")]
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
                FirstQuery = $"{time1}ms (DB)",
                SecondQuery = $"{time2}ms (Cache)",
                Improvement = $"{(time1 - time2)}ms faster"
            });
        }

        /// <summary>
        /// Tạo hàng loạt vocabulary
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/vocabulary/bulk
        ///     {
        ///         "vocabularies": [
        ///             {
        ///                 "text": "은행",
        ///                 "pronunciation": "eunhaeng",
        ///                 "definition": "ngân hàng",
        ///                 "exampleSentence": "은행에 가서 돈을 찾았어요.",
        ///                 "topicIds": ["topic_ngan_hang", "topic_dia_diem"]
        ///             },
        ///             {
        ///                 "text": "은행",
        ///                 "pronunciation": "eunhaeng",
        ///                 "definition": "quả ngân hạnh",
        ///                 "exampleSentence": "은행은 건강에 좋아요.",
        ///                 "topicIds": ["topic_thuc_vat"]
        ///             }
        ///         ]
        ///     }
        /// 
        /// </remarks>
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


        [HttpGet("search-for-user")]
        [AllowAnonymous] // Cho phép search không cần đăng nhập
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
        /// Cập nhật vocabulary
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PUT /api/vocabulary/{vocabularyId}
        ///     {
        ///         "pronunciation": "eunhaeng",
        ///         "definition": "ngân hàng (cập nhật)",
        ///         "exampleSentence": "새로운 예문",
        ///         "topicIds": ["topic_ngan_hang", "topic_dia_diem", "topic_doi_song"]
        ///     }
        /// 
        /// </remarks>
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
        /// <remarks>
        /// Ví dụ: GET /api/vocabulary/by-text?text=은행 sẽ trả về:
        /// - 은행 - ngân hàng (với topics: Ngân hàng, Địa điểm, Đời sống)
        /// - 은행 - quả ngân hạnh (với topics: Thực vật)
        /// </remarks>
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
        /// <remarks>
        /// Lấy tất cả vocabularies thuộc một topic cụ thể
        /// </remarks>
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
        /// <remarks>
        /// Tìm kiếm vocabularies theo text hoặc definition
        /// </remarks>
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

            // Sử dụng GetVocabulariesByTopic với searchText
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
    }
}
