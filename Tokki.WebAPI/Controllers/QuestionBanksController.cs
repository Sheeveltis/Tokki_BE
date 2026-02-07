using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Transcription;
using Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks;
using Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff;
using Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Delete;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update;
using Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.SubmitQuestionBankForApproval;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks;
using Tokki.Domain.Enums;

namespace Tokki.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuestionBanksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QuestionBanksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo mới một câu hỏi vào ngân hàng câu hỏi
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateQuestionBank([FromBody] CreateQuestionBankCommand command)
        {
            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                // Tuỳ convention của bạn: Unauthorized hoặc OperationResult Failure
                return Unauthorized(new { message = "Không xác định được người dùng từ token." });
            }

            command.CreateBy = userId.Trim();

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
  

        [HttpPost("staff")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CreateQuestionBankByStaff([FromBody] CreateQuestionBankByStaffCommand command)
        {
            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Không xác định được người dùng từ token." });

            command.CreateBy = userId.Trim();

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

    /// <summary>
    /// Cập nhật thông tin câu hỏi
    /// </summary>
    [HttpPut("update")]
        public async Task<IActionResult> UpdateQuestionBank([FromBody] UpdateQuestionBankCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        // POST: api/question-banks/submit-approval
        [HttpPut("submit-to-approval")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> SubmitForApproval([FromBody] SubmitApprovalRequest body)
        {
            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Không xác định được người dùng từ token." });

            var command = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = body?.QuestionBankIds ?? new List<string>(),
                SubmittedBy = userId.Trim()
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }


       
        [HttpPut("approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveQuestionBanks([FromBody] ApproveQuestionBanksCommand command)
        {
            // command.QuestionBankIds sẽ được bind từ JSON body
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // PUT: api/question-banks/reject
        [HttpPut("reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectQuestionBanks([FromBody] RejectQuestionBanksCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa câu hỏi theo ID
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteQuestionBank(string id)
        {
            var command = new DeleteQuestionBankCommand
            {
                QuestionBankId = id
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết câu hỏi theo ID
        /// </summary>
        [HttpGet("{id}")]

        public async Task<IActionResult> GetQuestionBankById(string id)
        {
            var query = new GetQuestionBankByIdQuery
            {
                QuestionBankId = id
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách câu hỏi (Tìm kiếm, Filter, Phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetQuestionBanks([FromQuery] GetQuestionBanksQuery query)
        {
            // query được bind tự động từ Query String nhờ [FromQuery]
            // Các tham số như SearchTerm, Skill, DifficultyLevel, PageNumber, PageSize...

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
        [HttpGet("question-type/{questionTypeId}")]
        public async Task<IActionResult> GetByQuestionTypeId(
            [FromRoute] string questionTypeId,
            [FromQuery] QuestionBankStatus? status,
            [FromQuery] string? createBy,
            [FromQuery] string? approvedBy)
        {
            var query = new GetQuestionBanksByQuestionTypeIdQuery
            {
                QuestionTypeId = questionTypeId?.Trim() ?? string.Empty,
                Status = status,
                CreateBy = string.IsNullOrWhiteSpace(createBy) ? null : createBy.Trim(),
                ApprovedBy = string.IsNullOrWhiteSpace(approvedBy) ? null : approvedBy.Trim()
            };

            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }


        [HttpPut("admin/activate")]
        public async Task<IActionResult> ActivateQuestionBanks(
            [FromBody] ActivateQuestionBanksCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// Thêm đáp án cho 1 câu hỏi
        /// </summary>
        [HttpPost("{questionBankId}/options")]
        public async Task<IActionResult> CreateOption(string questionBankId, [FromBody] CreateQuestionOptionCommand command)
        {
            command.QuestionBankId = questionBankId;
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật đáp án của 1 câu hỏi
        /// </summary>
        [HttpPut("{questionBankId}/options/{optionId}")]
        public async Task<IActionResult> UpdateOption(string questionBankId, string optionId, [FromBody] UpdateQuestionOptionCommand command)
        {
            command.QuestionBankId = questionBankId;
            command.OptionId = optionId;

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa cứng 1 đáp án của 1 câu hỏi
        /// </summary>
        [HttpDelete("{questionBankId}/options/{optionId}")]
        public async Task<IActionResult> DeleteOption(string questionBankId, string optionId)
        {
            var result = await _mediator.Send(new DeleteQuestionOptionCommand
            {
                QuestionBankId = questionBankId,
                OptionId = optionId
            });

            return StatusCode(result.StatusCode, result);
        }
    }
}