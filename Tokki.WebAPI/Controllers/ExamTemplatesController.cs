using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetAdminExamTemplates;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplates;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamTemplatesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExamTemplatesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách mẫu đề thi (phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetExamTemplates([FromQuery] GetExamTemplatesQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết mẫu đề thi theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamTemplateById(string id)
        {
            var query = new GetExamTemplateByIdQuery { ExamTemplateId = id };
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo mẫu đề thi mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateExamTemplate([FromBody] CreateExamTemplateCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật mẫu đề thi
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateExamTemplate(string id, [FromBody] UpdateExamTemplateCommand command)
        {
            if (id != command.ExamTemplateId)
            {
                return BadRequest(new { message = "ID không khớp" });
            }

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa mẫu đề thi
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteExamTemplate(string id)
        {
            // TODO: Implement DeleteExamTemplateCommand
            return NoContent();
        }

        //Trang Anh
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> GetAdminExamTemplates([FromQuery] GetAdminExamTemplatesQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }
    }
}
