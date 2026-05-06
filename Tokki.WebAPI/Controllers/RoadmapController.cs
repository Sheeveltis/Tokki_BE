using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.Common.Constants;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.Commands.CancelRoadmap;
using Tokki.Application.UseCases.Roadmap.Commands.CompleteTask;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Application.UseCases.Roadmap.Commands.ProcessWeeklyResult;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.UseCases.Roadmap.Queries;
using Tokki.Application.UseCases.Roadmap.Queries.GetCurrentWeekProgress;
using Tokki.Application.UseCases.Roadmap.Queries.GetEntranceExam;
using Tokki.Application.UseCases.Roadmap.Queries.GetEntranceFeedback;
using Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap;
using Tokki.Application.UseCases.Roadmap.Queries.GetVirtualQuiz;
using Tokki.Application.UseCases.Roadmap.Constants;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoadmapController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserRoadmapRepository _userRoadmapRepository;
        private readonly IRoadmapProgressService _progressService;
        private readonly ITopikLevelConfigService _topikConfig;

        public RoadmapController(
            IMediator mediator,
            IUserRoadmapRepository userRoadmapRepository,
            IRoadmapProgressService progressService,
            ITopikLevelConfigService topikConfig)
        {
            _mediator = mediator;
            _userRoadmapRepository = userRoadmapRepository;
            _progressService = progressService;
            _topikConfig = topikConfig;
        }

        [HttpGet("target-aims")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTargetAims(CancellationToken ct)
        {
            var levels = await _topikConfig.GetAllAsync(ct);

            var result = levels.Select(x => new
            {
                Value = x.TargetAimLevel,
                DisplayName = x.DisplayName,
                ExamGroup = x.ExamGroup,
                PassScore = x.PassScore,
                TotalScore = x.TotalScore,
                Listening = x.Listening,
                Reading = x.Reading,
                Writing = x.Writing,
                Strategy = x.Strategy
            }).ToList();

            return Ok(result);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRoadmap([FromBody] GenerateRoadmapDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var command = new GenerateRoadmapCommand
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

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentRoadmap()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var query = new GetRoadmapQuery(userId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess) return Ok(result);
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result);
        }

        [HttpGet("current-week-progress")]
        public async Task<IActionResult> GetCurrentWeekProgress()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var query = new GetCurrentWeekProgressQuery(userId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess) return Ok(result);
            if (result.StatusCode == 404) return NotFound(result);
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

        [HttpGet("task/{taskId}/exam")]
        public async Task<IActionResult> GetWeeklyExam(string taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _userRoadmapRepository
                .GetTaskByIdAsync(taskId, CancellationToken.None);

            if (task == null)
                return NotFound(new { message = "Không tìm thấy task." });

            if (task.RoadmapWeek.UserRoadmap.UserId != userId)
                return Forbid();

            if (task.TaskType != RoadmapTaskType.WeeklyExam)
                return BadRequest(new { message = "Task này không phải bài kiểm tra tuần." });

            if (string.IsNullOrEmpty(task.ExamId))
                return NotFound(new { message = "Đề thi cho tuần này chưa được tạo. Vui lòng thử lại sau." });

            return Ok(new { ExamId = task.ExamId });
        }
        [HttpPost("next-week")]
        public async Task<IActionResult> GenerateNextWeek([FromBody] GenerateNextWeekDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var command = new GenerateNextWeekCommand
            {
                UserId = userId,
                FinishedWeekId = request.FinishedWeekId
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return StatusCode(202, result);
        }

        [HttpGet("virtual-quiz/{questionTypeId}")]
        public async Task<IActionResult> GetVirtualQuiz(
            string questionTypeId,
            [FromQuery] int count = 10)
        {
            var query = new GetVirtualQuizQuery(questionTypeId, count);
            var result = await _mediator.Send(query);

            if (result.IsSuccess) return Ok(result);
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result);
        }

        [HttpGet("entrance-exam")]
        public async Task<IActionResult> GetEntranceExam([FromQuery] TargetAimLevel targetAim)
        {
            var query = new GetEntranceExamQuery { TargetAim = targetAim };
            var result = await _mediator.Send(query);

            if (result.IsSuccess) return Ok(result);
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result);
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelRoadmap()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var command = new CancelRoadmapCommand { UserId = userId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess) return Ok(result);
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result);
        }

        // [HttpPost("process-weekly-result")] — Deprecated: logic đã được merge vào GenerateNextWeek
        // EvaluateLastWeekPerformanceAsync trong GenerateNextWeekCommandHandler xử lý toàn bộ
        // public async Task<IActionResult> ProcessWeeklyResult(...) { ... }

        [HttpGet("entrance-feedback")]
        public async Task<IActionResult> GetEntranceFeedback(
            [FromQuery] string userExamId,
            [FromQuery] TargetAimLevel targetAim)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var query = new GetEntranceFeedbackQuery
            {
                UserId = userId,
                UserExamId = userExamId,
                TargetAim = targetAim
            };

            var result = await _mediator.Send(query);

            if (result.StatusCode == 202)
                return StatusCode(202, result);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }
        [HttpGet("task/{taskId}/detail")]
        public async Task<IActionResult> GetTaskDetail(string taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var query = new GetTaskDetailQuery
            {
                TaskId = taskId,
            };

            var result = await _mediator.Send(query);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpGet("progress/{jobId}")]
        public IActionResult GetProgress(string jobId)
        {
            var state = _progressService.Get(jobId);

            if (state == null)
                return NotFound(new { message = "Không tìm thấy job. Có thể đã hết hạn hoặc jobId không hợp lệ." });

            return Ok(state);
        }

        [HttpGet("week/{roadmapWeekId}/progress")]
        public async Task<IActionResult> GetWeekProgress(string roadmapWeekId)
        {
            var query = new GetWeekProgressQuery
            {
                RoadmapWeekId = roadmapWeekId,
            };

            var result = await _mediator.Send(query);

            if (result.StatusCode == 404) return NotFound(result);
            return Ok(result);
        }
    }
}