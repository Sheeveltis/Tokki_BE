// Tokki.API/Controllers/EmailCampaignController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailCampaign;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailCampaign;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaignById;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaigns;

namespace Tokki.API.Controllers
{
    [Route("api/email-campaigns")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class EmailCampaignController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmailCampaignController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ========== CREATE ==========
        /// <summary>
        /// Tạo chiến dịch gửi email (Gửi ngay hoặc Lên lịch)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmailCampaignByGroupCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // ========== GET ALL (PAGED + FILTER) ==========
        /// <summary>
        /// Lấy danh sách campaign (phân trang + filter)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetEmailCampaignsQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        // ========== GET BY ID ==========
        /// <summary>
        /// Lấy chi tiết 1 campaign theo id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            var result = await _mediator.Send(new GetEmailCampaignByIdQuery { JobId = id });
            return StatusCode(result.StatusCode, result);
        }

        // ========== UPDATE (ONLY PENDING) ==========
        /// <summary>
        /// Cập nhật campaign (chỉ cho Pending). Field rỗng/null sẽ giữ nguyên.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateEmailCampaignCommand command)
        {
            command.JobId = id;
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // ========== DELETE (SOFT DELETE, ONLY PENDING) ==========
        /// <summary>
        /// Xóa mềm campaign (chỉ cho Pending)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            var result = await _mediator.Send(new DeleteEmailCampaignCommand { JobId = id });
            return StatusCode(result.StatusCode, result);
        }
    }
}
