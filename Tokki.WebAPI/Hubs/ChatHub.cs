using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.WebAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _mongoChatService; 
        private readonly TokkiDbContext _sqlContext;    
        private readonly IIdGeneratorService _idGen;

        public ChatHub(
            IChatService mongoChatService,
            TokkiDbContext sqlContext,
            IIdGeneratorService idGen)
        {
            _mongoChatService = mongoChatService;
            _sqlContext = sqlContext;
            _idGen = idGen;
        }

        /// <summary>
        /// Hàm này Client gọi để gửi tin nhắn
        /// </summary>
        public async Task SendMessage(string roomId, string content)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Không xác định được người dùng.");
            }
            var userInfo = await _sqlContext.Accounts
                                .AsNoTracking()
                                .Where(u => u.UserId == userId)
                                .Select(u => new
                                {
                                    u.FullName,
                                    u.AvatarUrl
                                })
                                .FirstOrDefaultAsync();

            if (userInfo == null)
            {
                throw new HubException("Tài khoản không tồn tại trong hệ thống.");
            }

            var chatMsg = new LiveChatMessage
            {
                LiveChatMessageId = _idGen.Generate(),
                Content = content,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow,
                SenderId = userId,
                SenderName = userInfo.FullName ?? "Unknown User",
                SenderAvatar = userInfo.AvatarUrl ?? ""
            };
            await _mongoChatService.CreateMessageAsync(chatMsg);
            await Clients.Group(roomId).SendAsync("ReceiveMessage", chatMsg);
        }

        /// <summary>
        /// Hàm này Client gọi khi user bấm vào một phòng chat/lớp học
        /// </summary>
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        }
    }
}