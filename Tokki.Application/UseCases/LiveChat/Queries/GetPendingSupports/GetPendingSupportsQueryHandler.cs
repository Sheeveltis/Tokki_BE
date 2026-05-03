using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetPendingSupports
{
    public class GetPendingSupportsQueryHandler : IRequestHandler<GetPendingSupportsQuery, OperationResult<List<ChatRoomDTO>>>
    {
        private readonly IChatRoomRepository _repo;

        public GetPendingSupportsQueryHandler(IChatRoomRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<List<ChatRoomDTO>>> Handle(GetPendingSupportsQuery request, CancellationToken token)
        {
            var rooms = await _repo.GetPendingSupportRoomsAsync(token);

            var dtos = rooms.Select(r => {
                var customer = r.Members.FirstOrDefault()?.User;
                return new ChatRoomDTO
                {
                    ChatRoomId = r.ChatRoomId,
                    RoomName = customer?.FullName ?? "Khách ẩn danh",
                    RoomAvatar = customer?.AvatarUrl ?? "",
                    IsSupport = true,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();

            return OperationResult<List<ChatRoomDTO>>.Success(dtos);
        }
    }
}
