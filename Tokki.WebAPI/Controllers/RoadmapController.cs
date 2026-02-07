using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class RoadmapController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RoadmapController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRoadmap([FromBody] GenerateRoadmapDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }

            var command = new GenerateRoadmapCommand
            {
                UserId = userId,
                TargetAim = request.TargetAim,
                DurationDays = request.DurationDays,
                Weaknesses = request.Weaknesses,
                CurrentLevel = request.CurrentLevel
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GenerateRoadmap), new { id = result.Data }, result);
            }

            return BadRequest(result);
        }
    }
}