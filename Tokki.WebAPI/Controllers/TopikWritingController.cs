using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.TopikWriting.Commands.ClassifyAndSolve;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class TopikWritingController : ControllerBase
    {
        private readonly ISender _sender;

        public TopikWritingController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("classify-and-solve")]
        [AllowAnonymous] // test swagger cho nhanh; sau này đổi [Authorize]
        public async Task<IActionResult> ClassifyAndSolve([FromBody] ClassifyAndSolveTopikWritingCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
