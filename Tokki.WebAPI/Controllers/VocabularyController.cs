using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies;
using Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabulary;
using Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary;
using Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries; // Namespace chứa GetVocabularyByTextQuery (nếu có)
using Tokki.Application.UseCases.Vocabulary.Queries.FlashCard;
using Tokki.Application.UseCases.Vocabulary.Queries.GetAllForManager; // <--- THÊM DÒNG NÀY
using Tokki.Application.UseCases.Vocabulary.Queries.GetById;
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


        [HttpGet("user/get-detail/{vocabularyId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVocabularyDetail(string vocabularyId)
        {
            var query = new GetVocabularyDetailByIdQuery
            {
                VocabularyId = vocabularyId
            };

            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// Lấy danh sách vocabulary cho Manager (có filter, search, paging)
        /// </summary>
        /// <remarks>
        /// API này dùng cho trang quản lý, cho phép lọc theo status, topic, tìm kiếm.
        /// </remarks>
        [HttpGet("admin/get-all")]
        [Authorize(Roles = "Manager,Admin")] // Bạn có thể mở comment này nếu muốn chặn user thường
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllForManager(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] VocabularyStatus? status = null,
            [FromQuery] string? vocabId = null,
            [FromQuery] string? searchText = null)
        {
            var query = new GetAllForManagerQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                VocabId = vocabId,
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
        /// </summary>
        [HttpPost("admin/create-a-vocabulary")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateVocabularies([FromBody] CreateVocabularyCommand command)
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
        }  /// <summary>
           /// Cập nhật vocabulary
           /// </summary>
           /// <remarks>
           /// Sample request:
           /// 
           ///     PUT /api/vocabulary/{vocabularyId}
           ///     {
           ///         "pronunciation": "eunhaeng",
           ///         "definition": "ngân hàng (cập nhật)",
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
            // Giả định bạn đã có class GetVocabularyByTextQuery
            // Nếu chưa có class này, bạn cần tạo nó hoặc dùng GetAllForManagerQuery thay thế
            // Tạm thời comment code nếu class chưa tồn tại để tránh lỗi build
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
        [HttpGet("search-by-topic")]
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

    }
     

       
}