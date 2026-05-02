using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.LiveChat.Commands.CloseSupportChat
{
    public class CloseSupportChatCommandHandler : IRequestHandler<CloseSupportChatCommand, OperationResult<bool>>
    {
        private readonly IChatRoomRepository _roomRepo;
        private readonly IChatRepository _chatRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ISystemConfigRepository _configRepo;
        private readonly IIdGeneratorService _idGen;
        private readonly IChatNotificationService _notifier;

        public CloseSupportChatCommandHandler(
            IChatRoomRepository roomRepo,
            IChatRepository chatRepo,
            IAccountRepository accountRepo,
            ISystemConfigRepository configRepo,
            IIdGeneratorService idGen,
            IChatNotificationService notifier)
        {
            _roomRepo = roomRepo;
            _chatRepo = chatRepo;
            _accountRepo = accountRepo;
            _configRepo = configRepo;
            _idGen = idGen;
            _notifier = notifier;
        }

        public async Task<OperationResult<bool>> Handle(CloseSupportChatCommand request, CancellationToken token)
        {
            var room = await _roomRepo.GetRoomByIdAsync(request.RoomId, token);
            if (room == null) return OperationResult<bool>.Failure("Phòng chat không tồn tại.");

            if (!room.Members.Any(m => m.UserId == request.UserId))
            {
                return OperationResult<bool>.Failure("Bạn không có quyền đóng phòng chat này.");
            }

            room.IsClosed = true;

            await _roomRepo.UpdateRoomAsync(room);
            await _roomRepo.SaveChangesAsync(token);

            // Lấy template từ SystemConfig
            var template = await _configRepo.GetValueByKeyAsync("CHAT_CLOSE_MESSAGE_TEMPLATE");
            if (string.IsNullOrEmpty(template))
            {
                template = "Cuộc trò chuyện đã được đóng bởi {0}.";
            }

            // Gửi tin nhắn hệ thống báo đóng phòng
            var userInfo = await _accountRepo.GetBasicInfoAsync(request.UserId!);
            var closerName = userInfo?.FullName ?? "Người dùng";

            var systemMsg = new ChatMessage
            {
                ChatMessageId = _idGen.Generate(21),
                RoomId = request.RoomId,
                Content = string.Format(template, closerName),
                CreatedAt = DateTime.UtcNow,
                Type = ChatMessageType.System,
                SenderId = null
            };

            await _chatRepo.CreateMessageAsync(systemMsg);
            
            // Thông báo cho các bên qua SignalR
            systemMsg.SenderName = "Hệ thống";
            systemMsg.SenderAvatar = "";
            await _notifier.SendMessageToRoomAsync(request.RoomId, systemMsg);
            await _notifier.NotifyRoomClosedAsync(request.RoomId);

            return OperationResult<bool>.Success(true);
        }
    }
}
