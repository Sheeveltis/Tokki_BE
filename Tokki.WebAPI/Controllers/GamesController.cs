using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Games.Commands.SaveGameResult;
using Tokki.Application.UseCases.Games.Commands.UpdateGameResult;
using Tokki.Application.UseCases.Games.Queries.CheckUserPlayedLevel;
using Tokki.Application.UseCases.Games.Queries.GetGameLeaderboard;
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
        [HttpGet("user/has-played-level")]
        [Authorize]
        public async Task<IActionResult> HasPlayedLevel(
    [FromQuery] GameType gameType,
    [FromQuery] string? topicId,
    [FromQuery] GameDifficulty gameDifficulty)
        {
            var query = new CheckUserPlayedLevelQuery
            {
                GameType = gameType,
                TopicId = topicId,
                GameDifficulty = gameDifficulty
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("admin/get-game-results")]
        public async Task<IActionResult> GetGameResults(
            [FromQuery] string? userId = null,
            [FromQuery] GameType? gameType = null,
            [FromQuery] string? topicId = null,
            [FromQuery] GameDifficulty? diff = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                var query = new GetGameResultForUserQuery
                {
                    UserId = userId,
                    GameType = gameType,
                    TopicId = topicId,
                    GameDifficulty = diff,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var result = await _mediator.Send(query);
                return StatusCode(result.StatusCode, result);
            }
            else
            {
                var query = new GetGameResultsForAllUsersQuery
                {
                    GameType = gameType,
                    TopicId = topicId,
                    gameDifficulty = diff,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var result = await _mediator.Send(query);
                return StatusCode(result.StatusCode, result);
            }
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGameLeaderboard(
            [FromQuery] GameType? type = null,
            [FromQuery] GameDifficulty? diff = null,
            [FromQuery] string? topicId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetGameLeaderboardQuery
            {
                GameType = type,
                GameDifficulty = diff,
                TopicId = topicId,
                PageNumber = pageNumber,
                PageSize = pageSize
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
        //[HttpPut("user/result")]
        //[Authorize]
        //public async Task<IActionResult> UpdateGameResult([FromBody] UpdateGameResultCommand command)
        //{
        //    var result = await _mediator.Send(command);
        //    return StatusCode(result.StatusCode, result);
        //}
    }
}
