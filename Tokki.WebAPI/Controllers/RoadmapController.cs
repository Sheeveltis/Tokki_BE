using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.UseCases.Roadmap.Commands.CompleteTask;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap;
using Tokki.Domain.Enums;
using Tokki.Application.IServices; 
using Tokki.Infrastructure.Data;

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

            if (task == null) return NotFound("Task not found");

            var existingExam = await _context.Exams
                .FirstOrDefaultAsync(e => e.Title.Contains(taskId) && e.CreatedBy == "AI_SYSTEM");

            if (existingExam != null)
            {
                return Ok(new { ExamId = existingExam.ExamId, IsNew = false });
            }
            //var templateId = "TMP_TEST";
            var template = await _context.ExamTemplates
                .Where(t => t.Status == ExamTemplateStatus.Published) 
                .OrderByDescending(t => t.Name.Contains("Test"))    
                .ThenByDescending(t => t.CreatedAt)                
                .FirstOrDefaultAsync();

            if (template == null)
            {
                return BadRequest("Hệ thống chưa có cấu trúc đề thi nào (ExamTemplate). Vui lòng chạy Script tạo dữ liệu mẫu.");
            }

            var templateId = template.ExamTemplateId;

            DifficultyLevel level = DifficultyLevel.Easy;
            if (task.RoadmapWeek.UserRoadmap.TargetAim.Contains("II")) level = DifficultyLevel.Medium;

            var result = await _examAssemblyService.GenerateWeeklyExamAsync(
                templateId,
                userId,
                task.RoadmapWeek.WeekIndex,
                new List<string>(), 
                level,
                CancellationToken.None
            );

            if (!result.IsSuccess) return BadRequest(result.Message);

            var newExam = await _context.Exams.FindAsync(result.Data);
            if (newExam != null)
            {
                newExam.Title = $"Weekly Exam {task.RoadmapWeek.WeekIndex} - {taskId}"; 
                await _context.SaveChangesAsync();
            }

            return Ok(new { ExamId = result.Data, IsNew = true });
        }
    }
}