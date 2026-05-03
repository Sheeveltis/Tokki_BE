using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetAllActiveSupports
{
    public class GetAllActiveSupportsQueryHandler : IRequestHandler<GetAllActiveSupportsQuery, OperationResult<List<ChatRoomDTO>>>
    {
        private readonly IChatRoomRepository _repo;

        public GetAllActiveSupportsQueryHandler(IChatRoomRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<List<ChatRoomDTO>>> Handle(GetAllActiveSupportsQuery request, CancellationToken token)
        {
            // Lấy tất cả các phòng Support đang mở (không phân biệt đã có staff hay chưa)
            var rooms = await _repo.GetActiveSupportRoomsForAdminAsync(token);

            var dtos = rooms.Select(r => {
                var customer = r.Members.FirstOrDefault(m => m.User.Role == Tokki.Domain.Enums.AccountRole.User)?.User;
                var staff = r.Members.FirstOrDefault(m => m.User.Role == Tokki.Domain.Enums.AccountRole.Staff || m.User.Role == Tokki.Domain.Enums.AccountRole.Admin)?.User;
                
                return new ChatRoomDTO
                {
                    ChatRoomId = r.ChatRoomId,
                    RoomName = customer?.FullName ?? "Khách ẩn danh",
                    RoomAvatar = customer?.AvatarUrl ?? "",
                    IsSupport = true,
                    CreatedAt = r.CreatedAt,
                    StaffName = staff?.FullName // Trả thêm tên staff đang trực nếu có
                };
            }).ToList();

            return OperationResult<List<ChatRoomDTO>>.Success(dtos);
        }
    }
}
