using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Payments.Commands.ProcessWebhook;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ISender _sender;

        public WebhookController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("sepay")]
        public async Task<IActionResult> ReceiveSePayWebhook([FromBody] SePayWebhookData data)
        {
            var command = new ProcessWebhookCommand { Data = data };
            var result = await _sender.Send(command);

            return Ok(new { success = true, message = result.Message });
        }
    }
}