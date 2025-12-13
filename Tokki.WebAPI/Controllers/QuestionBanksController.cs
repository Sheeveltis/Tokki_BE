using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks;

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
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật thông tin câu hỏi
        /// </summary>
        [HttpPut("Update")]
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
    }
}