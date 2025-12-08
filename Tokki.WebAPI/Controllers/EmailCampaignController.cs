using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign;
using Tokki.Application.UseCases.Email.Commands.CreateCampaign; // Namespace chứa Command
using Tokki.Domain.Enums; // Namespace chứa Enum AccountRole

namespace Tokki.API.Controllers
{
    [Route("api/email-campaigns")] // Đường dẫn: api/email-campaigns
    [ApiController]
    public class EmailCampaignController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmailCampaignController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// API cho Admin tạo chiến dịch gửi email (Gửi ngay hoặc Lên lịch)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")] // Quan trọng: Chỉ Admin mới được phép gọi
        public async Task<IActionResult> CreateCampaign([FromBody] CreateEmailCampaignCommand command)
        {
            // Gọi sang Handler thông qua MediatR
            var result = await _mediator.Send(command);

            // Trả về kết quả (200 OK hoặc Lỗi)
            return StatusCode(result.StatusCode, result);
        }
    }
}