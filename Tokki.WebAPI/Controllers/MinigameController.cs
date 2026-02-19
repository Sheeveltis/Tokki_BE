using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleGuess;
using Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleSentence;
using Tokki.Application.UseCases.MiniGame.Queries.MatchingCard;
using Tokki.Application.UseCases.MiniGame.Queries.Solitaire;
using Tokki.Application.UseCases.MiniGame.Queries.Wordle;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/minigame")]
    [ApiController]
    public class MiniGameController : ControllerBase
    {
        private readonly ISender _sender;

        public MiniGameController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("matching-cards")]
        public async Task<IActionResult> GetMatchingCards([FromQuery] string topicId, [FromQuery] int quantity = 8)
        {
            var query = new GetMatchingCardsQuery
            {
                TopicId = topicId,
                Quantity = quantity
            };

            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("solitaire")]
        public async Task<IActionResult> GetSolitaire([FromQuery] int quantity = 52)
        {
            var query = new GetSolitaireTopicsQuery
            {
                Quantity = quantity
            };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("wordle/guess")]
        public async Task<IActionResult> PostWordleGuess([FromBody] SubmitWordleGuessCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.UserId = userId;
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("wordle/submit-sentence")]
        public async Task<IActionResult> SubmitSentence([FromBody] SubmitWordleSentenceCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.UserId = userId;

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("wordle")]
        public async Task<IActionResult> GetWordle()
        {
            var userId = User.FindFirst("UserId")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var query = new GetDailyWordleStatusQuery
            {
                UserId = userId
            };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("wordle/result/{dailyWordleId}")]
        public async Task<IActionResult> GetResult(string dailyWordleId)
        {
            var userId = User.FindFirst("UserId")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var query = new GetWordleResultQuery
            {
                DailyWordleId = dailyWordleId,
                UserId = userId
            };

            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        
    }
}