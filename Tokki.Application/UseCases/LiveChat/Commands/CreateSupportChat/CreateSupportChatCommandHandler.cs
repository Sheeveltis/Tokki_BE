using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.LiveChat.Commands.CreateSupportChat
{
    public class CreateSupportChatCommandHandler : IRequestHandler<CreateSupportChatCommand, OperationResult<string>>
    {
        private readonly IChatRoomRepository _chatRoomRepo;
        private readonly IIdGeneratorService _idGen;

        public CreateSupportChatCommandHandler(IChatRoomRepository chatRoomRepo, IIdGeneratorService idGen)
        {
            _chatRoomRepo = chatRoomRepo;
            _idGen = idGen;
        }

        public async Task<OperationResult<string>> Handle(CreateSupportChatCommand request, CancellationToken token)
        {
            var room = new ChatRoom
            {
                ChatRoomId = _idGen.Generate(10),
                Name = "Hỗ trợ tư vấn",
                IsGroup = true,
                IsSupport = true,
                IsClosed = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var member = new ChatRoomMember
            {
                ChatRoomMemberId = _idGen.Generate(15),
                ChatRoomId = room.ChatRoomId,
                UserId = request.UserId,
                IsAdmin = true,
                JoinedAt = DateTimeOffset.UtcNow
            };

            await _chatRoomRepo.AddRoomAsync(room);
            await _chatRoomRepo.AddMemberAsync(member);

            await _chatRoomRepo.SaveChangesAsync(token);

            return OperationResult<string>.Success(room.ChatRoomId);
        }
    }
}