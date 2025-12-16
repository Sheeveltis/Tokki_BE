using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Leaderboard.Queries;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaderboardController : ControllerBase
    {
        private readonly ISender _sender;

        public LeaderboardController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaderboard(
            [FromQuery] LeaderboardTimeFrame timeFrame = LeaderboardTimeFrame.AllTime,
            [FromQuery] int top = 20)
        {
            var query = new GetLeaderboardQuery
            {
                TimeFrame = timeFrame,
                Top = top
            };

            var result = await _sender.Send(query);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }
    }
}