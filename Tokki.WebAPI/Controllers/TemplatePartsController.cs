using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.TemplateParts.Commands.CreateTemplatePart;
using Tokki.Application.UseCases.TemplateParts.Commands.DeleteTemplatePart; // Giả sử bạn đã có file Delete
using Tokki.Application.UseCases.TemplateParts.Commands.UpdateTemplatePart;

namespace Tokki.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplatePartsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TemplatePartsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTemplatePartCommand command)
        {
            var result = await _mediator.Send(command);
            return result.IsSuccess ? CreatedAtAction(nameof(Create), new { id = result.Data }, result) : StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTemplatePartCommand command)
        {
            if (id != command.TemplatePartId) return BadRequest("Mismatched ID");
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteTemplatePartCommand(id);
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }
    }
}