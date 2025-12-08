// Tokki.API/Controllers/EmailTemplateController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.EmailTemplates.Commands;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Queries;
using Tokki.Infrastructure.BackgroundJobs;

namespace Tokki.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class EmailTemplateController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly AutomationWorker _automationWorker;

        public EmailTemplateController(IMediator mediator, AutomationWorker automationWorker)
        {
            _mediator = mediator;
            _automationWorker = automationWorker; // ✅ THÊM dòng này
        } // ✅ ĐÓNG constructor tại đây

        // GET: api/emailtemplate
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllEmailTemplatesQuery());
            return StatusCode(result.StatusCode, result);
        }

        // GET: api/emailtemplate/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetEmailTemplateByIdQuery { TemplateId = id });
            return StatusCode(result.StatusCode, result);
        }

        // POST: api/emailtemplate
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmailTemplateCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // PUT: api/emailtemplate/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEmailTemplateCommand command)
        {
            command.TemplateId = id;
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // DELETE: api/emailtemplate/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteEmailTemplateCommand { TemplateId = id });
            return StatusCode(result.StatusCode, result);
        }

        // ✅ TEST ENDPOINT
        [HttpPost("test-offline-email")]
        [AllowAnonymous] // ✅ Cho phép test không cần auth (hoặc bỏ nếu chỉ Admin)
        public async Task<IActionResult> TestOfflineEmail()
        {
            try
            {
                await _automationWorker.RunDailyTasks();
                return Ok(new { message = "✅ Email automation đã chạy thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Lỗi", error = ex.Message });
            }
        }
    }
}