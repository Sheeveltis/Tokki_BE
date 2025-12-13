using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Favorite.Commands.AddFavoriteTopic;
using Tokki.Application.UseCases.Favorite.Commands.AddFavoriteWord;
using Tokki.Application.UseCases.Favorite.Commands.RemoveFavoriteTopic;
using Tokki.Application.UseCases.Favorite.Commands.RemoveFavoriteWord;
using Tokki.Application.UseCases.Favorite.Queries.GetFavoriteTopics;
using Tokki.Application.UseCases.Favorite.Queries.GetFavoriteWords;
using Tokki.Domain.Enums;

namespace Tokki.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FavoritesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Favorite Words

        /// <summary>
        /// Thêm từ vựng vào danh sách yêu thích
        /// </summary>
        [HttpPost("words")]
        public async Task<IActionResult> AddFavoriteWord([FromBody] AddFavoriteWordCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách từ vựng yêu thích
        /// </summary>
        [HttpGet("words")]
        public async Task<IActionResult> GetFavoriteWords(
            [FromQuery] string? searchTerm,
            [FromQuery] UserFavoriteWordStatus? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetFavoriteWordsQuery
            {
                SearchTerm = searchTerm,
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
        /// Bỏ từ vựng khỏi danh sách yêu thích
        /// </summary>
        [HttpDelete("words/{wordId}")]
        public async Task<IActionResult> RemoveFavoriteWord(
            string wordId,
            [FromQuery] bool forceDelete = false)
        {
            var command = new RemoveFavoriteWordCommand
            {
                WordId = wordId,
                ForceDelete = forceDelete
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        #endregion

        #region Favorite Topics

        /// <summary>
        /// Thêm chủ đề vào danh sách yêu thích
        /// </summary>
        [HttpPost("topics")]
        public async Task<IActionResult> AddFavoriteTopic([FromBody] AddFavoriteTopicCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách chủ đề yêu thích
        /// </summary>
        [HttpGet("topics")]
        public async Task<IActionResult> GetFavoriteTopics(
            [FromQuery] string? searchTerm,
            [FromQuery] UserFavoriteTopicStatus? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetFavoriteTopicsQuery
            {
                SearchTerm = searchTerm,
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
        /// Bỏ chủ đề khỏi danh sách yêu thích
        /// </summary>
        [HttpDelete("topics/{topicId}")]
        public async Task<IActionResult> RemoveFavoriteTopic(
            string topicId,
            [FromQuery] bool forceDelete = false)
        {
            var command = new RemoveFavoriteTopicCommand
            {
                TopicId = topicId,
                ForceDelete = forceDelete
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        #endregion
    }
}