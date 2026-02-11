using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationRule.Commands.EvaluatePronunciation;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PronunciationController : ControllerBase
    {
        private readonly ISender _sender;

        public PronunciationController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("evaluate")]
        [Authorize] 
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Evaluate([FromForm] EvaluatePronunciationCommand command)
        {
            var result = await _sender.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
