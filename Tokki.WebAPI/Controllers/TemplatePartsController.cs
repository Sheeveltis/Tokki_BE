using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.TemplateParts.Commands.CreateTemplatePart;
using Tokki.Application.UseCases.TemplateParts.Commands.DeleteTemplatePart; 
using Tokki.Application.UseCases.TemplateParts.Commands.UpdateTemplatePart;
using Tokki.Application.UseCases.TemplateParts.Queries.GetTemplatePartById;

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
            command.TemplatePartId = id;
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var query = new GetTemplatePartByIdQuery { TemplatePartId = id };
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
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