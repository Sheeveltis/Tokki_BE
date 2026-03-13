using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.IRepositories;
using Tokki.Application.Common.Constants;
using Tokki.Application.UseCases.Exam.Commands.SubmitExam;
using Tokki.Application.UseCases.Roadmap.Commands.CompleteTask;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.UseCases.Roadmap.Queries.GetDurationRecommendation;
using Tokki.Application.UseCases.Roadmap.Queries.GetRoadmap;
using Tokki.Application.UseCases.Roadmap.Queries.GetVirtualQuiz;
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

        public RoadmapController(
            IMediator mediator,
            IUserRoadmapRepository userRoadmapRepository) 
        {
            _mediator = mediator;
            _userRoadmapRepository = userRoadmapRepository;
        }
     
        [HttpGet("target-aims")]
        [AllowAnonymous]
        public IActionResult GetTargetAims()
        {
            var result = TopikLevelConfig.Levels
                .Select(kvp => new
                {
                    Value = (int)kvp.Key,
                    EnumName = kvp.Key.ToString(),
                    DisplayName = kvp.Value.DisplayName,
                    ExamGroup = kvp.Value.ExamGroup,
                    PassScore = kvp.Value.PassScore,
                    TotalScore = kvp.Value.TotalScore
                })
                .OrderBy(x => x.Value)
                .ToList();

            return Ok(result);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRoadmap([FromBody] GenerateRoadmapDTO request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var existingRoadmap = await _userRoadmapRepository
                .GetActiveRoadmapByUserIdAsync(userId, CancellationToken.None);

            if (existingRoadmap != null)
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
                return CreatedAtAction(nameof(GenerateRoadmap), new { id = result.Data }, result);

            return BadRequest(result);
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

            if (result.StatusCode == 200 && result.Message?.Contains("hoàn thành") == true)
                return Ok(new { message = result.Message, isFinished = true });

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(new
            {
                message = "Đã tạo lộ trình tuần mới thành công!",
                isFinished = false,
                hasWarning = result.Data?.HasWarning ?? false,
                warningMessage = result.Data?.WarningMessage,
                persistentWeakTypeIds = result.Data?.PersistentWeakTypeIds ?? new List<string>()
            });
        }

        [HttpPost("submit-exam")]
        public async Task<IActionResult> SubmitExam([FromBody] SubmitExamRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

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

            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
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

        [HttpGet("duration-recommendation")]
        public async Task<IActionResult> GetDurationRecommendation(
            [FromQuery] string examId,
            [FromQuery] TargetAimLevel targetAim,
            [FromQuery] List<string> weakTypeIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            var query = new GetDurationRecommendationQuery
            {
                UserId = userId,
                ExamId = examId,
                TargetAim = targetAim,
                WeakQuestionTypeIds = weakTypeIds ?? new List<string>()
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }
    }
}