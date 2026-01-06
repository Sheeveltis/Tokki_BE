// File: Tokki.API/Controllers/QuestionTypeController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.QuestionTypes.Commands.CreateQuestionType;
using Tokki.Application.UseCases.QuestionTypes.Commands.DeleteQuestionType;
using Tokki.Application.UseCases.QuestionTypes.Commands.UpdateQuestionType;
using Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypes;
using Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypeById;
using Tokki.Domain.Enums;

namespace Tokki.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bảo mật API
    public class QuestionTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QuestionTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] QuestionSkill? skill,
            [FromQuery] DifficultyLevel? difficulty,
            [FromQuery] ExamType? examType)
        {
            var query = new GetQuestionTypesQuery
            {
                Keyword = keyword,
                Skill = skill,
                Difficulty = difficulty,
                ExamType = examType
            };
            var result = await _mediator.Send(query);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var query = new GetQuestionTypeByIdQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                if (result.StatusCode == 404)
                    return NotFound(result); 

                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateQuestionTypeCommand command)
        {
            var result = await _mediator.Send(command);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data }, result)
                : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateQuestionTypeCommand command)
        {
            command.QuestionTypeId = id;
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteQuestionTypeCommand(id);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.StatusCode == 404)
                    return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}