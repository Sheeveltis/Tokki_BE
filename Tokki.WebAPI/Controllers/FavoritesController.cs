using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.AddFavoriteVocabulary;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.RemoveFavoriteVocabulary;
using Tokki.Application.UseCases.FavoriteVocabulary.Queries.GetFavoriteVocabularies;
using Tokki.Domain.Enums;

namespace Tokki.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class FavoritesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FavoritesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddFavoriteVocabularyCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites([FromQuery] GetFavoriteVocabulariesQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete]
        public async Task<IActionResult> Remove([FromBody] RemoveFavoriteVocabularyCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}