using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.LiveChat.Commands.JoinSupportChat
{
    public class JoinSupportChatCommandHandler : IRequestHandler<JoinSupportChatCommand, OperationResult<bool>>
    {
        private readonly IChatRoomRepository _roomRepo;
        private readonly IChatRepository _chatRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IIdGeneratorService _idGen;
        private readonly IChatNotificationService _notifier;
        public JoinSupportChatCommandHandler(
            IChatRoomRepository roomRepo,
            IChatRepository chatRepo,
            IAccountRepository accountRepo,
            IIdGeneratorService idGen,
            IChatNotificationService notifier) 
        {
            _roomRepo = roomRepo;
            _chatRepo = chatRepo;
            _accountRepo = accountRepo;
            _idGen = idGen;
            _notifier = notifier;
        }

        public async Task<OperationResult<bool>> Handle(JoinSupportChatCommand request, CancellationToken token)
        {
            var room = await _roomRepo.GetRoomByIdAsync(request.RoomId, token);
            if (room == null) return OperationResult<bool>.Failure(AppErrors.ChatRoomNotFound, 404);

            if (room.Members.Count > 1)
            {
                if (room.Members.Any(m => m.UserId == request.StaffId))
                    return OperationResult<bool>.Success(true);

                return OperationResult<bool>.Failure(AppErrors.ChatRoomAlreadySupported, 400);
            }

            var staffInfo = await _accountRepo.GetBasicInfoAsync(request.StaffId);
            string staffName = staffInfo?.FullName ?? "Nhân viên hỗ trợ";

            var member = new ChatRoomMember
            {
                ChatRoomMemberId = _idGen.Generate(15),
                ChatRoomId = request.RoomId,
                UserId = request.StaffId,
                IsAdmin = true,
                JoinedAt = DateTimeOffset.UtcNow
            };

            await _roomRepo.AddMemberAsync(member);
            await _roomRepo.SaveChangesAsync(token);

            var systemMsg = new ChatMessage
            {
                ChatMessageId = _idGen.Generate(21),
                RoomId = request.RoomId,
                Content = $"{staffName} đã tham gia cuộc trò chuyện.",
                CreatedAt = DateTime.UtcNow,
                Type = ChatMessageType.System,
                SenderId = null
            };

            await _chatRepo.CreateMessageAsync(systemMsg);
            systemMsg.SenderName = "Hệ thống";
            systemMsg.SenderAvatar = "";
            await _notifier.SendMessageToRoomAsync(request.RoomId, systemMsg);
            return OperationResult<bool>.Success(true);
        }
    }
}