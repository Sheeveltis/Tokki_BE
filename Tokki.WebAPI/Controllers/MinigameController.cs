using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.MiniGame.Queries.MatchingCard;
using Tokki.Application.UseCases.MiniGame.Queries.Solitaire;

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
    }
}