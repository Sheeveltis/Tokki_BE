using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts;
using Tokki.Application.UseCases.ExamTemplates.Commands.ApproveExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.DeleteExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.RejectExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.SubmitExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplateStatus;
using Tokki.Application.UseCases.ExamTemplates.Commands.UpdateTemplatePart;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetAdminExamTemplates;
using Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById;

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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateExamTemplate([FromBody] CreateExamTemplateCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("TemplateParts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddTemplateParts([FromBody] AddTemplatePartsCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/duplicate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DuplicateExamTemplate(string id)
        {
            var command = new DuplicateExamTemplateCommand(id);
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateExamTemplate(string id, [FromBody] UpdateExamTemplateCommand command)
        {
            command.ExamTemplateId = id;
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("TemplateParts/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTemplatePart(string id, [FromBody] UpdateExamTemplatePartCommand command)
        {
            command.TemplatePartId = id;
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateExamTemplateStatus(string id, [FromBody] UpdateExamTemplateStatusCommand command)
        {
            command.ExamTemplateId = id;
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("{id}/submit")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SubmitForApproval(string id)
        {
            var command = new SubmitExamTemplateCommand { ExamTemplateId = id };
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveExamTemplate(string id)
        {
            var command = new ApproveExamTemplateCommand { ExamTemplateId = id };
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectExamTemplate(string id, [FromBody] RejectReasonDto dto)
        {
            var command = new RejectExamTemplateCommand
            {
                ExamTemplateId = id,
                Reason = dto.Reason
            };
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminExamTemplates([FromQuery] GetAdminExamTemplatesQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamTemplateById(string id)
        {
            var query = new GetExamTemplateByIdQuery { ExamTemplateId = id };
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteExamTemplate(string id)
        {
            var command = new DeleteExamTemplateCommand(id);
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}