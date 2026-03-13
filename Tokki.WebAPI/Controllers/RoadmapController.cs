using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Exam.Commands.SubmitExam;
using Tokki.Application.UseCases.Roadmap.Commands.CompleteTask;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;
using Tokki.Application.UseCases.Roadmap.Queries.GetVirtualQuiz;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class RoadmapController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IExamAssemblyService _examAssemblyService;
        private readonly TokkiDbContext _context;
        public RoadmapController(
            IMediator mediator, 
            IExamAssemblyService examAssemblyService,
            TokkiDbContext context)
        {
            _mediator = mediator;
            _examAssemblyService = examAssemblyService;
            _context = context;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRoadmap([FromBody] GenerateRoadmapDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }
            var existingRoadmap = await _context.UserRoadmaps
            .AnyAsync(r => r.UserId == userId && r.CurrentStatus == Tokki.Domain.Enums.UserRoadmapStatus.Active);

            if (existingRoadmap)
                return BadRequest(new
                {
                    message = "Bạn đang có một lộ trình học đang hoạt động. Vui lòng hoàn thành hoặc hủy lộ trình cũ trước khi tạo mới."
                });
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

        [HttpGet("task/{taskId}/exam")]
        public async Task<IActionResult> GetWeeklyExam(string taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.RoadmapDailyTasks
                .Include(t => t.RoadmapWeek)
                    .ThenInclude(w => w.UserRoadmap)
                .FirstOrDefaultAsync(t => t.TaskId == taskId);

            if (task == null)
                return NotFound(new { message = "Không tìm thấy task." });

            if (task.RoadmapWeek.UserRoadmap.UserId != userId)
                return Forbid();

            if (task.TaskType != Tokki.Domain.Enums.RoadmapTaskType.WeeklyExam)
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
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }

            var command = new GenerateNextWeekCommand
            {
                UserId = userId,
                FinishedWeekId = request.FinishedWeekId
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Đã tạo lộ trình tuần mới thành công!", status = 200 });
            }

            if (result.StatusCode == 200 && result.Message.Contains("hoàn thành"))
            {
                return Ok(new { message = result.Message, isFinished = true }); 
            }

            return BadRequest(result);
        }

        [HttpPost("submit-exam")]
        public async Task<IActionResult> SubmitExam([FromBody] SubmitExamRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }

            var command = new SubmitExamCommand
            {
                ExamId = request.ExamId,
                UserId = userId,
                Answers = request.Answers.Select(a => new UserAnswerDto
                {
                    QuestionId = a.QuestionId,
                    SelectedOptionId = a.SelectedOptionId
                }).ToList()
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result); 
            }

            return BadRequest(result);
        }

        [HttpGet("virtual-quiz/{questionTypeId}")]
        public async Task<IActionResult> GetVirtualQuiz(string questionTypeId, [FromQuery] int count = 10)
        {
            var query = new GetVirtualQuizQuery(questionTypeId, count);
            var result = await _mediator.Send(query);

            if (result.IsSuccess) return Ok(result);
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result);
        }
    }
}