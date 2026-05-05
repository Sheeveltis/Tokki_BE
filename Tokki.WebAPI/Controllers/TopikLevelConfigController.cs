using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.TopikLevelConfigs.Commands.Create;
using Tokki.Application.UseCases.TopikLevelConfigs.Commands.Delete;
using Tokki.Application.UseCases.TopikLevelConfigs.Commands.Update;
using Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetAll;
using Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetById;
using Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetTargetLevelByScore;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopikLevelConfigController : ControllerBase
    {
        private readonly ISender _sender;

        public TopikLevelConfigController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllForUser([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _sender.Send(new GetAllTopikLevelConfigsQuery 
            { 
                PageNumber = pageNumber, 
                PageSize = pageSize,
                IsActive = true
            });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> GetAllForAdmin(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchText = null,
            [FromQuery] int? examGroup = null,
            [FromQuery] bool? isActive = null)
        {
            var result = await _sender.Send(new GetAllTopikLevelConfigsQuery 
            { 
                PageNumber = pageNumber, 
                PageSize = pageSize,
                SearchText = searchText,
                ExamGroup = examGroup,
                IsActive = isActive
            });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _sender.Send(new GetTopikLevelConfigByIdQuery { Id = id });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> Create([FromBody] CreateTopikLevelConfigCommand command)
        {
            if (command == null) return BadRequest("Dữ liệu không hợp lệ.");
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> Update([FromBody] UpdateTopikLevelConfigCommand command)
        {
            if (command == null) return BadRequest("Dữ liệu không hợp lệ.");
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _sender.Send(new DeleteTopikLevelConfigCommand { Id = id });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("target-level")]
        public async Task<IActionResult> GetTargetLevel([FromQuery] int score, [FromQuery] int examGroup)
        {
            var result = await _sender.Send(new GetTargetLevelByScoreQuery { Score = score, ExamGroup = examGroup });
            return StatusCode(result.StatusCode, result);
        }
    }
}
