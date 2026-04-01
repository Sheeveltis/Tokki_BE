using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationExample.Commands.CreatePronunciationExample;
using Tokki.Application.UseCases.PronunciationExample.Commands.DeletePronunciationExample;
using Tokki.Application.UseCases.PronunciationExample.Commands.UpdatePronunciationExample;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExampleDetail;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExamplesByRuleId;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetPagedPronunciationExamples;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PronunciationExampleController : ControllerBase
    {
        private readonly ISender _sender;

        public PronunciationExampleController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] GetPagedPronunciationExamplesQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _sender.Send(new GetExampleDetailQuery(id));
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("rules/{ruleId}/examples")]
        public async Task<IActionResult> GetByRuleId(string ruleId)
        {
            var result = await _sender.Send(new GetExamplesByRuleIdQuery(ruleId));
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Create([FromBody] CreatePronunciationExampleCommand command)
        {
            command.UserId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePronunciationExampleCommand command)
        {
            command.ExampleId = id;
            command.UserId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _sender.Send(new DeletePronunciationExampleCommand { ExampleId = id });
            return StatusCode(result.StatusCode, result);
        }
    }
}
