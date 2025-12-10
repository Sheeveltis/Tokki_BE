using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Titles.Commands.CreateTitle;
using Tokki.Application.UseCases.Titles.Commands.DeleteTitle;
using Tokki.Application.UseCases.Titles.Commands.UpdateTitle;
using Tokki.Application.UseCases.Titles.Queries.GetAllTitles;
using Tokki.Application.UseCases.Titles.Queries.GetTitleById;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize(Roles = "Admin")] 
    public class TitleController : ControllerBase
    {
        private readonly ISender _sender;

        public TitleController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTitleCommand command)
        {
            var result = await _sender.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(201, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _sender.Send(new GetAllTitlesQuery());

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _sender.Send(new GetTitleByIdQuery(id));

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTitleCommand command)
        {
            if (id != command.TitleId)
            {
                command.TitleId = id;
            }

            var result = await _sender.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _sender.Send(new DeleteTitleCommand(id));

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}