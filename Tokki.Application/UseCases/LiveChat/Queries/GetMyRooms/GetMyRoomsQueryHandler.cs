using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetMyRooms
{
    public class GetMyRoomsQueryHandler : IRequestHandler<GetMyRoomsQuery, OperationResult<List<ChatRoomDTO>>>
    {
        private readonly IChatRoomRepository _repo;

        public GetMyRoomsQueryHandler(IChatRoomRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<List<ChatRoomDTO>>> Handle(GetMyRoomsQuery request, CancellationToken token)
        {
            var rooms = await _repo.GetUserRoomsAsync(request.UserId, token);
            var dtos = new List<ChatRoomDTO>();

            foreach (var room in rooms)
            {
                string displayName = room.Name ?? "Phòng chat";
                string displayAvatar = "";

                if (!room.IsGroup || room.IsSupport)
                {
                    var otherMember = room.Members.FirstOrDefault(m => m.UserId != request.UserId);
                    if (otherMember != null)
                    {
                        displayName = otherMember.User.FullName;
                        displayAvatar = otherMember.User.AvatarUrl;
                    }
                }

                dtos.Add(new ChatRoomDTO
                {
                    ChatRoomId = room.ChatRoomId,
                    RoomName = displayName,
                    RoomAvatar = displayAvatar,
                    IsSupport = room.IsSupport,
                    IsGroup = room.IsGroup,
                    CreatedAt = room.CreatedAt
                });
            }

            return OperationResult<List<ChatRoomDTO>>.Success(dtos);
        }
    }
}
