using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Reports.Commands.CreateReport;
using Tokki.Application.UseCases.Reports.Commands.DeleteReport;
using Tokki.Application.UseCases.Reports.Commands.MarkReportRead;
using Tokki.Application.UseCases.Reports.Commands.UpdateReportStatus;
using Tokki.Application.UseCases.Reports.Queries.GetAllReports;
using Tokki.Application.UseCases.Reports.Queries.GetReportNotifications;
using Tokki.Domain.Entities;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReportCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ReportStatus? status)
        {
            var query = new GetAllReportsQuery { Status = status };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("Vui lòng nhập userId để test");

            var query = new GetReportNotificationsQuery { UserId = userId };
            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkRead(string id, [FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("Vui lòng nhập userId để test");

            var command = new MarkReportReadCommand { ReportId = id, UserId = userId };
            var result = await _mediator.Send(command);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateReportStatusCommand command)
        {
            command.ReportId = id;

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("Vui lòng nhập userId");

            var command = new DeleteReportCommand
            {
                ReportId = id,
                UserId = userId,
                IsAdmin = false 
            };

            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}