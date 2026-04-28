using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.EnumConfigs.Commands.Create;
using Tokki.Application.UseCases.EnumConfigs.Commands.Update;
using Tokki.Application.UseCases.EnumConfigs.Queries.GetAll;
using Tokki.Application.UseCases.EnumConfigs.Queries.GetByGroup;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/enums")]
    [ApiController]
    public class EnumConfigController : ControllerBase
    {
        private readonly ISender _sender;

        public EnumConfigController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("lookup/{groupCode}")]
        public async Task<IActionResult> GetLookup(EnumGroup groupCode)
        {
            var query = new GetEnumConfigByGroupQuery(groupCode);
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> GetAll([FromQuery] EnumGroup? groupCode)
        {
            var query = new GetAllEnumConfigsQuery { GroupCode = groupCode };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> Create([FromBody] CreateEnumConfigCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> Update([FromBody] UpdateEnumConfigCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
