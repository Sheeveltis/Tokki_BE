using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.LiveChat.Commands.JoinSupportChat
{
    public class JoinSupportChatCommandHandler : IRequestHandler<JoinSupportChatCommand, OperationResult<bool>>
    {
        private readonly IChatRoomRepository _repo;
        private readonly IIdGeneratorService _idGen;

        public JoinSupportChatCommandHandler(IChatRoomRepository repo, IIdGeneratorService idGen)
        {
            _repo = repo;
            _idGen = idGen;
        }

        public async Task<OperationResult<bool>> Handle(JoinSupportChatCommand request, CancellationToken token)
        {
            var room = await _repo.GetRoomByIdAsync(request.RoomId, token);
            if (room == null) return OperationResult<bool>.Failure(AppErrors.ChatRoomNotFound, 404);

            if (room.Members.Count > 1)
            {
                if (room.Members.Any(m => m.UserId == request.StaffId))
                    return OperationResult<bool>.Success(true);

                return OperationResult<bool>.Failure(AppErrors.ChatRoomAlreadySupported, 400);
            }

            var member = new ChatRoomMember
            {
                ChatRoomMemberId = _idGen.Generate(15),
                ChatRoomId = request.RoomId,
                UserId = request.StaffId,
                IsAdmin = false,
                JoinedAt = DateTimeOffset.UtcNow
            };

            await _repo.AddMemberAsync(member);

            await _repo.SaveChangesAsync(token);

            return OperationResult<bool>.Success(true);
        }
    }
}
