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
using Tokki.Application.UseCases.Exam.Queries.GetExamsStats;
using Tokki.Application.UseCases.Exam.Queries.GetExamDetailStats;
using Tokki.Application.UseCases.Exam.Queries.GetUserExamsByExamId;
using Tokki.Application.UseCases.Exam.Queries.GetQuestionsByPart;
using Tokki.Application.UseCases.Exam.Queries.GetTemplateSkills;
using Tokki.Application.UseCases.Exam.Commands.ExportExamToPdf;
using Tokki.Application.UseCases.Exam.Queries.GetTrialExams;
using Tokki.Application.UseCases.UserExam.Commands.CreateUserTakeExam;
using Tokki.Domain.Enums;

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
        [HttpGet("{examTemplateId}/skills")]
        public async Task<ActionResult<OperationResult<List<string>>>> GetSkills(string examTemplateId)
        {
            var result = await _sender.Send(new GetTemplateSkillsQuery { TemplateId = examTemplateId });
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamCommand command)
        {
            if (command == null)
            {
                return BadRequest("Dữ liệu đầu vào không hợp lệ hoặc sai định dạng JSON.");
            }

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
     
        [HttpGet("trial-exams")]
        public async Task<IActionResult> GetTrialExams([FromQuery] GetTrialExamsQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllExamsForAdmin([FromQuery] GetExamsQuery query )
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin/stats")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetExamsStats([FromQuery] GetExamsStatsQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin/stats/{examId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetExamsStatsDetail(string examId)
        {
            var result = await _sender.Send(new GetExamDetailStatsQuery(examId));
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin/stats/{examId}/participants")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetExamParticipants(
            string examId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] ExamParticipantSortBy sortBy = ExamParticipantSortBy.SubmitTime,
            [FromQuery] bool isDescending = true)
        {
            var result = await _sender.Send(new GetUserExamsByExamIdQuery 
            { 
                ExamId = examId, 
                PageNumber = pageNumber, 
                PageSize = pageSize,
                SortBy = sortBy,
                IsDescending = isDescending
            });
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
       
        [HttpPost("{id}/export-pdf")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ExportExamToPdf(string id, [FromQuery] bool showExplanation = false)
        {
            var command = new ExportExamToPdfCommand(id, showExplanation);
            var result = await _sender.Send(command);

            if (result.IsSuccess)
            {
                // Trả về file PDF cho Frontend tải xuống
                return File(result.Data.PdfData, "application/pdf", result.Data.FileName);
            }

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
