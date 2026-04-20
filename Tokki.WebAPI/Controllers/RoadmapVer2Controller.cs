using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.RoadmapVer2.Queries.GetCurrentRoadmap;
using Tokki.Application.UseCases.RoadmapVer2.Queries.GetTaskDetail;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoadmapVer2Controller : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IRoadmapProgressService _progressService;

        public RoadmapVer2Controller(IMediator mediator, IRoadmapProgressService progressService)
        {
            _mediator = mediator;
            _progressService = progressService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRoadmapV2([FromBody] Tokki.Application.UseCases.Roadmap.DTOs.GenerateRoadmapDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var command = new Tokki.Application.UseCases.RoadmapVer2.Commands.GenerateRoadmap.GenerateRoadmapCommand
            {
                UserId = userId,
                TargetAim = request.TargetAim,
                DurationDays = request.DurationDays,
                UserExamId = request.UserExamId
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(202, result);
        }

        [HttpGet("progress/{jobId}")]
        public IActionResult GetProgress(string jobId)
        {
            var progress = _progressService.Get(jobId);
            if (progress == null)
                return NotFound(new { message = "Không tìm thấy tiến trình hoặc mã đã hết hạn." });

            return Ok(progress);
        }

        //[HttpGet("current")]
        //public async Task<IActionResult> GetCurrentRoadmap()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var query = new GetCurrentRoadmapVer2Query(userId);
        //    var result = await _mediator.Send(query);

        //    if (result.IsSuccess) return Ok(result);
        //    if (result.StatusCode == 404) return NotFound(result);
        //    return BadRequest(result);
        //}

        //[HttpGet("task/{taskId}")]
        //public async Task<IActionResult> GetTaskDetail(string taskId)
        //{
        //    var query = new GetRoadmapVer2TaskDetailQuery(taskId);
        //    var result = await _mediator.Send(query);

        //    if (result.IsSuccess) return Ok(result);
        //    if (result.StatusCode == 404) return NotFound(result);
        //    return BadRequest(result);
        //}

        [HttpPost("next-week")]
        public async Task<IActionResult> GenerateNextWeekVer2([FromBody] Tokki.Application.UseCases.Roadmap.DTOs.GenerateNextWeekDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var command = new Tokki.Application.UseCases.RoadmapVer2.Commands.GenerateNextWeek.GenerateNextWeekCommand
            {
                UserId = userId,
                FinishedWeekId = request.FinishedWeekId
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result);

            return StatusCode(202, result);
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelRoadmapV2()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var command = new Tokki.Application.UseCases.RoadmapVer2.Commands.CancelRoadmap.CancelRoadmapCommand
            {
                UserId = userId
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
