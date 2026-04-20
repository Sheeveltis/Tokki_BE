using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.UserTopicProgress.Commands.CompleteTopic;
using Tokki.Application.UseCases.VocabSpacedRepetition.Commands.SubmitReview;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Application.UseCases.VocabSpacedRepetition.Queries.GetDueReviews;
using Tokki.Application.UseCases.VocabSpacedRepetition.Queries.GetPaginatedDueReviews;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SpacedRepetitionController : ControllerBase
    {
        private readonly ISender _sender;

        public SpacedRepetitionController(ISender sender)
        {
            _sender = sender;
        }
        /// <summary>
        /// Gửi kết quả học (Nhớ/Quên) cho 1 từ vựng
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.UserId = userId;
            var result = await _sender.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
        //[HttpPost("complete-topic")]
        //public async Task<IActionResult> CompleteTopic([FromBody] CompleteTopicCommand command)
        //{
        //    var userId = User.FindFirst("UserId")?.Value
        //                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized("Không xác định được người dùng.");
        //    }
        //    command.UserId = userId;

        //    var result = await _sender.Send(command);
        //    if (result.IsSuccess)
        //    {
        //        return Ok(result);
        //    }

        //    return StatusCode(result.StatusCode, result);
        //}
          [HttpGet("vocab-for-review-paginated")]
        public async Task<IActionResult> GetPaginatedVocabularyForReview([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var query = new GetPaginatedDueReviewsQuery
            {
                UserId = userId,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("vocab-for-review")]
        public async Task<IActionResult> GetNextVocabularyForReview(int limit = 100)
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var query = new GetDueReviewsQuery
            {
                UserId = userId,
                Limit = limit
            };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

      
    }
}