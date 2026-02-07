using Microsoft.AspNetCore.SignalR;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.WebAPI.Hubs;

namespace Tokki.WebAPI.Services
{
    public class ChatNotificationService : IChatNotificationService
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatNotificationService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendMessageToRoomAsync(string roomId, ChatMessage message)
        {
            await _hubContext.Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }
    }
}