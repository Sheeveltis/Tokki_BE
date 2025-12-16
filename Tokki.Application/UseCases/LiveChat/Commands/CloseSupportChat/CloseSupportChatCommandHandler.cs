using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Microsoft.AspNetCore.SignalR;

namespace Tokki.Application.UseCases.LiveChat.Commands.CloseSupportChat
{
    public class CloseSupportChatCommandHandler : IRequestHandler<CloseSupportChatCommand, OperationResult<bool>>
    {
        private readonly IChatRoomRepository _repo;

        public CloseSupportChatCommandHandler(IChatRoomRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<bool>> Handle(CloseSupportChatCommand request, CancellationToken token)
        {
            var room = await _repo.GetRoomByIdAsync(request.RoomId, token);
            if (room == null) return OperationResult<bool>.Failure("Phòng chat không tồn tại.");

            if (!room.Members.Any(m => m.UserId == request.UserId))
            {
                return OperationResult<bool>.Failure("Bạn không có quyền đóng phòng chat này.");
            }

            room.IsClosed = true;

            await _repo.UpdateRoomAsync(room);
            await _repo.SaveChangesAsync(token);
            return OperationResult<bool>.Success(true);
        }
    }
}
