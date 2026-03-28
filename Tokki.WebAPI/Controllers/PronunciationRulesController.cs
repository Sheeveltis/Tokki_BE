using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.PronunciationRule.Commands.CreatePronunciationRule;
using Tokki.Application.UseCases.PronunciationRule.Commands.DeletePronunciationRule;
using Tokki.Application.UseCases.PronunciationRule.Commands.UpdatePronunciationRule;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRuleById;
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
        [HttpGet]
        public async Task<IActionResult> GetPronunciationRules([FromQuery] GetPronunciationRulesQuery query)
        {
            var result = await _sender.Send(query);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _sender.Send(new GetPronunciationRuleByIdQuery(id));
            if (!result.IsSuccess)
            {
                return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateRule([FromBody] CreatePronunciationRuleCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            command.CreateBy = userId;
            var result = await _sender.Send(command);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateRule(string id, [FromBody] UpdatePronunciationRuleCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            command.PronunciationRuleId = id;
            command.UpdateBy = userId;
            var result = await _sender.Send(command);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteRule(string id)
        {
            var result = await _sender.Send(new DeletePronunciationRuleCommand(id));
            if (!result.IsSuccess)
            {
                return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
            }
            return Ok(result);
        }
    }
}
