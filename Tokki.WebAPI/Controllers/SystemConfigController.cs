using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.SystemConfigs.Commands.Create;
using Tokki.Application.UseCases.SystemConfigs.Commands.Update;
using Tokki.Application.UseCases.SystemConfigs.Queries.GetAll;
using Tokki.Application.UseCases.SystemConfigs.Queries.GetSystemConfigByKey;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.Excel.Commands.ImportSystemConfigs;
using Tokki.Application.UseCases.Excel.Queries.ExportSystemConfigs;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/system-configs")] // Đặt tên theo chuẩn REST (số nhiều, gạch nối)
    [ApiController]
    public class SystemConfigController : ControllerBase
    {
        private readonly ISender _sender; // Dùng ISender gọn hơn IMediator

        public SystemConfigController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Admin))]

        public async Task<IActionResult> CreateConfig([FromBody] CreateSystemConfigCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        [Authorize(Roles = nameof(AccountRole.Admin))]

        public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        
        [HttpGet]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> GetAllConfigs(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] SystemConfigType? configType = null,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAllSystemConfigsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                ConfigType = configType,
                SearchTerm = search,
                IsActive = isActive
            };

            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetConfigByKey(string key)
        {
            var query = new GetSystemConfigByKeyQuery(key);
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
 
        [HttpGet("export")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> Export()
        {
            var result = await _sender.Send(new ExportSystemConfigsQuery());
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
 
            return File(result.Data!.FileContent, result.Data.ContentType, result.Data.FileName);
        }
 
        [HttpPost("import")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var command = new ImportSystemConfigsCommand { File = file };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}