using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Tokki.Application.UseCases.LiveChat.Commands.CloseSupportChat;
using Tokki.Application.UseCases.LiveChat.Commands.CreateSupportChat;
using Tokki.Application.UseCases.LiveChat.Commands.JoinSupportChat;
using Tokki.Application.UseCases.LiveChat.Queries.GetChatHistory;
using Tokki.Application.UseCases.LiveChat.Queries.GetMyRooms;
using Tokki.Application.UseCases.LiveChat.Queries.GetPendingSupports;
using Tokki.Application.UseCases.LiveChat.Queries.GetActiveSupportRoom;
using Tokki.Application.UseCases.LiveChat.Queries.GetClosedSupportRooms;
using Tokki.Application.UseCases.LiveChat.Queries.GetAllActiveSupports;
using Tokki.WebAPI.Hubs;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IHubContext<ChatHub> _hubContext;
        public ChatController(ISender sender, IHubContext<ChatHub> hubContext)
        {
            _sender = sender;
            _hubContext = hubContext;
        }

        [HttpPost("support/request")]
        public async Task<IActionResult> RequestSupport()
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _sender.Send(new CreateSupportChatCommand { UserId = userId! });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("support/{roomId}/join")]
        public async Task<IActionResult> JoinSupport(string roomId)
        {
            var staffId = User.FindFirst("UserId")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var command = new JoinSupportChatCommand
            {
                RoomId = roomId,
                StaffId = staffId!
            };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("my-rooms")]
        public async Task<IActionResult> GetMyRooms()
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _sender.Send(new GetMyRoomsQuery { UserId = userId! });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("support/pending")]
        public async Task<IActionResult> GetPendingSupport()
        {
            var result = await _sender.Send(new GetPendingSupportsQuery());
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("{roomId}/history")]
        public async Task<IActionResult> GetChatHistory(string roomId)
        {
            var result = await _sender.Send(new GetChatHistoryQuery { RoomId = roomId });
            return StatusCode(result.StatusCode, result);
        }
        
        [HttpGet("support/active-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetActiveSupportsForAdmin()
        {
            var result = await _sender.Send(new GetAllActiveSupportsQuery());
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("support/active")]
        public async Task<IActionResult> GetActiveSupportRoom()
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _sender.Send(new GetActiveSupportRoomQuery { UserId = userId! });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("support/history")]
        public async Task<IActionResult> GetSupportHistory([FromQuery] int days = 7, [FromQuery] string? search = null)
        {
            var result = await _sender.Send(new GetClosedSupportRoomsQuery { Days = days, Search = search });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("support/{roomId}/close")]
        public async Task<IActionResult> CloseSupportChat(string roomId)
        {
            var userId = User.FindFirst("UserId")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var command = new CloseSupportChatCommand
            {
                RoomId = roomId,
                UserId = userId
            };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}