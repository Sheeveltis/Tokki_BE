using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Transcription;
using Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff;
using Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Delete;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
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

        /// <summary>
        /// Xóa câu hỏi theo ID
        /// </summary>
        [HttpDelete("{id}")]
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
            string questionTypeId,
            [FromQuery] QuestionBankStatus? status)
            {
                var query = new GetQuestionBanksByQuestionTypeIdQuery
                {
                    QuestionTypeId = questionTypeId,
                    Status = status
                };

                var result = await _mediator.Send(query);
                return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
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