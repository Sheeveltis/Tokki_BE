using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetClosedSupportRooms
{
    public class GetClosedSupportRoomsQueryHandler : IRequestHandler<GetClosedSupportRoomsQuery, OperationResult<List<ChatRoomDTO>>>
    {
        private readonly IChatRoomRepository _repo;

        public GetClosedSupportRoomsQueryHandler(IChatRoomRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<List<ChatRoomDTO>>> Handle(GetClosedSupportRoomsQuery request, CancellationToken token)
        {
            var rooms = await _repo.GetClosedSupportRoomsAsync(request.Days, request.Search, token);

            var dtos = rooms.Select(r => {
                // Member đầu tiên thường là khách hàng (hoặc dùng logic check IsSupport)
                var customer = r.Members.OrderBy(m => m.JoinedAt).FirstOrDefault()?.User;
                var staff = r.Members.OrderBy(m => m.JoinedAt).Skip(1).FirstOrDefault()?.User;

                return new ChatRoomDTO
                {
                    ChatRoomId = r.ChatRoomId,
                    RoomName = customer?.FullName ?? "Khách ẩn danh",
                    RoomAvatar = customer?.AvatarUrl ?? "",
                    IsSupport = true,
                    IsClosed = true,
                    CreatedAt = r.CreatedAt,
                    StaffName = staff?.FullName
                };
            }).ToList();

            return OperationResult<List<ChatRoomDTO>>.Success(dtos);
        }
    }
}
