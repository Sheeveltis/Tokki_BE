using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam;
using Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam;
using Tokki.Application.UseCases.UserExam.Commands.SyncExamProgress;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.GetInProgressExam;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamReview;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExams;

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
        [Authorize]
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
        [HttpPost("sync-progress")]
        public async Task<ActionResult<OperationResult<bool>>> SyncProgress([FromBody] SyncExamProgressCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var result = await _sender.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("user/submit")]
        public async Task<ActionResult<OperationResult<SubmitExamResponse>>> SubmitExam([FromBody] SubmitUserExamCommand command)
        {
            var result = await _sender.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
        [HttpGet("user/history")]
        public async Task<ActionResult<OperationResult<UserTakeExamResponse>>> GetHistoryExam([FromQuery] GetUserExamsQuery command)
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
        [HttpGet("user/review-exam")]
        public async Task<ActionResult<OperationResult<UserExamReviewResponse>>> GetUserExamReview([FromQuery] GetUserExamReviewQuery command)
        {
            var result = await _sender.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
        
    }
}
