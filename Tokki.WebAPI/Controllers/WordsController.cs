using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Word.Commands.BulkCreateWords;
using Tokki.Application.UseCases.Word.Commands.DeleteWord; // Mới thêm
using Tokki.Application.UseCases.Word.Commands.UpdateWord; // Mới thêm
using Tokki.Application.UseCases.Word.Queries;
using Tokki.Application.UseCases.Word.Queries.GetWordsByTopicQuery;
using Tokki.Domain.Enums;

namespace Tokki.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WordsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WordsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreateWords([FromBody] BulkCreateWordsCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return StatusCode(result.StatusCode, result);
        }

        // --- API MỚI: UPDATE ---
        [HttpPut("Update")]
        public async Task<IActionResult> UpdateWord( [FromBody] UpdateWordCommand command)
        {
            // Gán ID từ URL vào Command để đảm bảo chính xác
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        // --- API MỚI: DELETE ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWord(string id, [FromQuery] bool forceDelete = false)
        {
            var command = new DeleteWordCommand
            {
                WordId = id,
                ForceDelete = forceDelete
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpGet("meanings")]
        public async Task<IActionResult> GetWordMeanings(
            [FromQuery] string? wordId,
            [FromQuery] string? text,
            [FromQuery] string? topicId,
            [FromQuery] MeaningStatus? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetWordMeaningsQuery
            {
                WordId = wordId,
                Text = text,
                TopicId = topicId,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }


        /// <summary>
        /// Lấy danh sách từ vựng theo TopicId với phân trang và filter
        /// </summary>
        /// <param name="topicId">ID của topic (required)</param>
        /// <param name="status">Status để filter: Active=1, Inactive=0, Deleted=2 (optional)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (optional)</param>
        /// <param name="pageNumber">Số trang (default: 1)</param>
        /// <param name="pageSize">Kích thước trang (default: 10)</param>
        [HttpGet("{topicId}/words")]
        public async Task<IActionResult> GetWordsByTopic(
            string topicId,
            [FromQuery] WordStatus? status,
            [FromQuery] string? searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetWordsByTopicQuery
            {
                TopicId = topicId,
                Status = status,
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}