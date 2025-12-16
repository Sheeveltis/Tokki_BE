using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentById;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentHistory;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentQr;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ISender _sender;

        public PaymentController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var query = new GetPaymentByIdQuery { Id = id };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}/qr")]
        public async Task<IActionResult> GetQrCode(string id)
        {
            var query = new GetPaymentQrQuery(id);
            var result = await _sender.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("history-token-user")]
        //[Authorize] 
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }

            var query = new GetPaymentHistoryQuery(userId);
            var result = await _sender.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin/{userId}/history")]
        // [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> GetHistoryByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Vui lòng cung cấp User ID.");
            }

            var query = new GetPaymentHistoryQuery(userId);
            var result = await _sender.Send(query);

            return StatusCode(result.StatusCode, result);
        }
    }
}