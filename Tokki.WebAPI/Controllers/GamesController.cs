using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Games.Commands.SaveGameResult;
using Tokki.Application.UseCases.Games.Queries.GetAllGamesForUser;
using Tokki.Application.UseCases.Games.Queries.GetGameResultForUser;
using Tokki.Application.UseCases.Games.Queries.GetGameResultsForAllUsers;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GamesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GamesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách game cho user (phân trang).
        /// </summary>
        [HttpGet("user/get-all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllGamesForUser(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] GameType? gameType = null)
        {
            var query = new GetAllGamesForUserQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                GameType = gameType
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("user/get-all-user-results")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGameResultsForAllUsers(
    [FromQuery] string gameId,
    [FromQuery] string topicId,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
        {
            var query = new GetGameResultsForAllUsersQuery
            {
                GameId = gameId,
                TopicId = topicId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }/// <summary>
         /// Lấy kết quả game của 1 user (best score, latest score, thời gian chơi)
         /// </summary>
        [HttpGet("user/get-user-results")]
        [Authorize] // hoặc cho admin tùy bạn
        public async Task<IActionResult> GetGameResultForUser(
            [FromQuery] string gameId,
            [FromQuery] string topicId,
            [FromQuery] string userId)
        {
            var query = new GetGameResultForUserQuery
            {
                GameId = gameId,
                TopicId = topicId,
                UserId = userId
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("user/save-result")]
        [Authorize]
        public async Task<IActionResult> SaveGameResult([FromBody] SaveGameResultCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

    }
}
