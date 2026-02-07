using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.Commands.AddQuestionToExam;
using Tokki.Application.UseCases.Exam.Commands.CreateExam;
using Tokki.Application.UseCases.Exam.Commands.DeleteExam;
using Tokki.Application.UseCases.Exam.Commands.RegenerateExamPart;
using Tokki.Application.UseCases.Exam.Commands.RemoveQuestionFromExam;
using Tokki.Application.UseCases.Exam.Commands.UpdateExamInfo;
using Tokki.Application.UseCases.Exam.Commands.UpdateExamStatus;
using Tokki.Application.UseCases.Exam.Queries.GetExamById;
using Tokki.Application.UseCases.Exam.Queries.GetExamDetailQuery;
using Tokki.Application.UseCases.Exam.Queries.GetExams;
using Tokki.Application.UseCases.Exam.Queries.GetQuestionsByPart;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamsController : ControllerBase
    {
        private readonly ISender _sender;

        public ExamsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            command.CreatedBy = userId;
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("regenerate-part")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> RegenerateExamPart([FromBody] RegenerateExamPartCommand command)
        {
            var result = await _sender.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllExamsForAdmin([FromQuery] GetExamsQuery query )
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("admin/detail")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetExamDetail(string examId)
        {
            var query = new GetExamDetailQuery { ExamId = examId };

            var result = await _sender.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("get-questions-by-part")] 
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAvailableQuestions(
            string templatePartId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = new GetQuestionsByPartQuery
            {
                TemplatePartId = templatePartId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search
            };

            var result = await _sender.Send(query);

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
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateExamInfo(string id, [FromBody] UpdateExamInfoCommand command)
        {
            command.ExamId = id;
            var result = await _sender.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateExamStatusCommand command)
        {
            command.ExamId = id;
            var result = await _sender.Send(command);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }
        [HttpPut("update-exam-question")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateExamQuestions([FromBody] AddQuestionToExamCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
       
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteExam(string id)
        {
            var command = new DeleteExamCommand { ExamId = id };
            var result = await _sender.Send(command);

            if (result.IsSuccess) return Ok(result);
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result);
        }
    }
}
