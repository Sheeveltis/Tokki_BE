using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.PronunciationRule.Commands.CreatePronunciationRule;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRules;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PronunciationRulesController : ControllerBase
    {
        private readonly ISender _sender;
        public PronunciationRulesController(ISender sender)
        {
            _sender = sender;
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRule([FromBody] CreatePronunciationRuleCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.CreateBy = userId!;

            var result = await _sender.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        [HttpGet("rules")]
        public async Task<IActionResult> GetPronunciationRules()
        {
            var query = new GetPronunciationRulesQuery();
            var result = await _sender.Send(query);

            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
