using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Alphabet.Commands.CreateAlphabet;
using Tokki.Application.UseCases.Alphabet.Commands.DeleteAlphabet;
using Tokki.Application.UseCases.Alphabet.Commands.ToggleAlphabetStatus;
using Tokki.Application.UseCases.Alphabet.Commands.UpdateAlphabet;
using Tokki.Application.UseCases.Alphabet.Queries;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlphabetController : ControllerBase
    {
        private readonly ISender _sender;

        public AlphabetController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AlphabetType? type, [FromQuery] bool? isActive)
        {
            var result = await _sender.Send(new GetAlphabetAllQuery { Type = type, IsActive = isActive });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("paginated")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> GetPaginated([FromQuery] GetAlphabetPaginatedQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _sender.Send(new GetAlphabetDetailQuery(id));
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Create([FromBody] CreateAlphabetCommand command)
        {
            if (command == null) return BadRequest("Dữ liệu không hợp lệ.");
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAlphabetCommand command)
        {
            if (command == null) return BadRequest("Dữ liệu không hợp lệ.");
            if (id != command.Id)
            {
                return BadRequest("ID không khớp.");
            }
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _sender.Send(new DeleteAlphabetCommand(id));
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _sender.Send(new ToggleAlphabetStatusCommand(id));
            return StatusCode(result.StatusCode, result);
        }
    }
}
