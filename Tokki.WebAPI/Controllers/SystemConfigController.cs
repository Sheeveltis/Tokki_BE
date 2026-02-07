using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.SystemConfigs.Commands.Create;
using Tokki.Application.UseCases.SystemConfigs.Commands.Update;
using Tokki.Application.UseCases.SystemConfigs.Queries.GetAll;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/system-configs")] // Đặt tên theo chuẩn REST (số nhiều, gạch nối)
    [ApiController]
    [Authorize(Roles = nameof(AccountRole.Admin))] // 🔒 Chỉ Admin mới được can thiệp cấu hình hệ thống
    public class SystemConfigController : ControllerBase
    {
        private readonly ISender _sender; // Dùng ISender gọn hơn IMediator

        public SystemConfigController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> CreateConfig([FromBody] CreateSystemConfigCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAllConfigs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetAllSystemConfigsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

    //    [HttpGet("{key}")]
    //    public async Task<IActionResult> GetConfigByKey(string key)
    //    {
    //        var query = new GetSystemConfigByKeyQuery(key);
    //        var result = await _sender.Send(query);
    //        return StatusCode(result.StatusCode, result);
    //    }
    }
}