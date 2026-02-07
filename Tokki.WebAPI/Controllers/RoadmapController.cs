using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Roadmap.Commands.CompleteTask;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap;

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

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentRoadmap()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }

            var query = new GetRoadmapQuery(userId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            if (result.StatusCode == 404)
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteTask([FromBody] CompleteTaskRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var command = new CompleteTaskCommand
            {
                TaskId = request.TaskId,
                UserId = userId,
                Performance = request.Performance
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }
    }
}