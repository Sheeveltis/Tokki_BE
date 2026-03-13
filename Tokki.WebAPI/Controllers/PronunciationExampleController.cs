using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExampleDetail;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExamplesByRuleId;

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
        [HttpGet("rules/{ruleId}/examples")]
        public async Task<IActionResult> GetExamplesByRuleId(string ruleId)
        {
            var query = new GetExamplesByRuleIdQuery(ruleId);
            var result = await _sender.Send(query);

            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("examples/{exampleId}")]
        public async Task<IActionResult> GetExampleDetail(string exampleId)
        {
            var query = new GetExampleDetailQuery(exampleId);
            var result = await _sender.Send(query);

            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
