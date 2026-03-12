using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam;
using Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam;
using Tokki.Application.UseCases.UserExam.Commands.SyncMCQProgress;
using Tokki.Application.UseCases.UserExam.Commands.SyncWritingProgress;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.CheckGradingStatus;
using Tokki.Application.UseCases.UserExam.Queries.GetExamAnalysis;
using Tokki.Application.UseCases.UserExam.Queries.GetInProgressExam;
using Tokki.Application.UseCases.UserExam.Queries.GetListeningDetail;
using Tokki.Application.UseCases.UserExam.Queries.GetReadingDetail;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamReview;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExams;
using Tokki.Application.UseCases.UserExam.Queries.GetWritingDetail;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserExamController : ControllerBase
    {
        private readonly ISender _sender;

        public UserExamController(ISender sender)
        {
            _sender = sender;
        }
        [HttpPost("user/take-exam")]
        public async Task<IActionResult> UserTakeExam(string examId, bool isShuffle = false)
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            var query = new CreateUserTakeExamCommand
            {
                ExamId = examId,
                IsShuffle = isShuffle,
                UserId = userId
            };

            var result = await _sender.Send(query);

            if (result.IsSuccess) return Ok(result);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("sync/mcq")]
        public async Task<IActionResult> SyncMCQ([FromBody] SyncMCQProgressCommand command)
        {
            command ??= new SyncMCQProgressCommand();
            var userId = User.FindFirst("UserId")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.UserId = userId;
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("sync/writing")]
        public async Task<IActionResult> SyncWriting([FromBody] SyncWritingProgressCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.UserId = userId;
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("user/submit")]
        public async Task<ActionResult<OperationResult<SubmitExamResponse>>> SubmitExam([FromBody] SubmitUserExamCommand command)
        {

            var userId = User.FindFirst("UserId")?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.UserId = userId;
            var result = await _sender.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
        [HttpGet("user/history")]
        public async Task<IActionResult> GetHistoryExam([FromQuery] string? examId, [FromQuery] UserExamStatus? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst("UserId")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized("Không xác định người dùng.");

            var command = new GetUserExamsQuery
            {
                UserId = userId,
                ExamId = examId,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("user/detail/in-progress")]
        public async Task<ActionResult<OperationResult<UserTakeExamResponse>>> GetInProgressExam([FromQuery] GetInProgressExamQuery command)
        {

            var result = await _sender.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
        [HttpGet("{userExamId}/is-graded")]
        public async Task<IActionResult> IsGraded(string userExamId)
        {
            var result = await _sender.Send(new CheckGradingStatusQuery { UserExamId = userExamId });
            return Ok(result.Data?.IsGraded ?? false);
        }
        [HttpGet("{userExamId}/result")]
        public async Task<IActionResult> GetExamResultOverview(string userExamId)
        {
            var query = new GetUserExamResultQuery { UserExamId = userExamId };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("{userExamId}/result/listening")]
        public async Task<IActionResult> GetListeningDetail(string userExamId)
        {
            var query = new GetListeningDetailQuery { UserExamId = userExamId };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{userExamId}/result/reading")]
        public async Task<IActionResult> GetReadingDetail(string userExamId)
        {
            var query = new GetReadingDetailQuery { UserExamId = userExamId };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{userExamId}/result/writing")]
        public async Task<IActionResult> GetWritingDetail(string userExamId)
        {
            var query = new GetWritingDetailQuery { UserExamId = userExamId };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("{userExamId}/analysis")]
        public async Task<IActionResult> GetExamAnalysis(string userExamId)
        {
            var query = new GetExamAnalysisQuery(userExamId);
            var result = await _sender.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
