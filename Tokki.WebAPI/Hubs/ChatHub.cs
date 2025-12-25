using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.WebAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatRepository _mongoChatService;
        private readonly IAccountRepository _accountRepo;
        private readonly IIdGeneratorService _idGen;

        public ChatHub(
            IChatRepository mongoChatService,
            IAccountRepository accountRepo,
            IIdGeneratorService idGen)
        {
            _mongoChatService = mongoChatService;
            _accountRepo = accountRepo;
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

            var userInfo = await _accountRepo.GetBasicInfoAsync(userId);

            if (userInfo == null)
            {
                throw new HubException("Tài khoản không tồn tại trong hệ thống.");
            }
            var chatMsg = new ChatMessage
            {
                ChatMessageId = _idGen.Generate(21),
                Content = content,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow,
                SenderId = userId,
                SenderName = userInfo.FullName,
                SenderAvatar = userInfo.AvatarUrl ?? "", 
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