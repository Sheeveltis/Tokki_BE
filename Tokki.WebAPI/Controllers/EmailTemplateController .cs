// Tokki.API/Controllers/EmailTemplateController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetAllEmailTemplates;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailAutoTemplateById;
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
            _automationWorker = automationWorker;
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmailAutoTemplateCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllEmailAutoTemplatesQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetEmailAutoTemplateByIdQuery { TemplateId = id });
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateEmailAutoTemplateCommand command)
        {
            

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // DELETE (soft delete)
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteEmailAutoTemplateCommand { TemplateId = id });
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // TEST (manual trigger)
        // =========================
        [HttpPost("test-automation")]
        [AllowAnonymous] // nếu chỉ Admin thì bỏ dòng này
        public async Task<IActionResult> TestAutomation()
        {
            try
            {
                await _automationWorker.RunDailyTasks();
                return Ok(new { message = "Email automation đã chạy thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi", error = ex.Message });
            }
        }
    }
}
