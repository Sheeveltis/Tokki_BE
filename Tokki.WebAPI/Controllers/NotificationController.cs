using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Notifications.Commands.MarkAllAsRead;
using Tokki.Application.UseCases.Notifications.Commands.MarkAsRead;
using Tokki.Application.UseCases.Notifications.Queries.GetMyNotifications;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly ISender _sender;

        public NotificationController(ISender sender)
        {
            _sender = sender;
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
        }

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var query = new GetMyNotificationsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                UserId = userId
            };

            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("mark-as-read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var command = new MarkAsReadCommand
            {
                NotificationId = id,
                UserId = userId
            };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var command = new MarkAllAsReadCommand
            {
                UserId = userId
            };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
