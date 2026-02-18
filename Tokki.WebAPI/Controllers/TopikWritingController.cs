using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.TopikWriting.Commands.ClassifyAndSolve;
using Tokki.Application.UseCases.TopikWriting.Question51.Commands;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question52.Commands;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question53.Commands;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question54.Commands;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;

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
        [HttpPost("question51")]
        public async Task<IActionResult> SolveQuestion51(
     [FromBody] Question51RequestDto dto,
     CancellationToken ct)
        {
            var command = new SolveQuestion51Command { Payload = dto };
            var result = await _sender.Send(command, ct);

            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpPost("question52")]
        public async Task<IActionResult> SolveQuestion52(
    [FromBody] Question52RequestDto dto,
    CancellationToken ct)
        {
            var command = new SolveQuestion52Command { Payload = dto };
            var result = await _sender.Send(command, ct);

            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }
        [HttpPost("question53")]
        public async Task<IActionResult> SolveQuestion53(
    [FromBody] Question53RequestDto dto,
    CancellationToken ct)
        {
            var command = new SolveQuestion53Command { Payload = dto };
            var result = await _sender.Send(command, ct);

            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }
        [HttpPost("question54")]
        public async Task<IActionResult> SolveQuestion54(
    [FromBody] Question54RequestDto dto,
    CancellationToken ct)
        {
            var command = new SolveQuestion54Command { Payload = dto };
            var result = await _sender.Send(command, ct);

            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }
    }
}
