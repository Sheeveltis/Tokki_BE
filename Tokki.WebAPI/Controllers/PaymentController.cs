using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentById;

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
    }
}