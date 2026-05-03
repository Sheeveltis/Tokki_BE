using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IChatRoomRepository
    {
        // 1. Tìm phòng chat 1-1 giữa 2 người (để không tạo trùng)
        Task<ChatRoom?> GetPrivateRoomAsync(string user1Id, string user2Id, CancellationToken token = default);

        // 2. Lấy danh sách phòng chat của một User (kèm tin nhắn cuối - xử lý sau ở tầng Application nếu cần)
        Task<List<ChatRoom>> GetUserRoomsAsync(string userId, CancellationToken token = default);

        // 3. Lấy thông tin chi tiết 1 phòng (kèm Members)
        Task<ChatRoom?> GetRoomByIdAsync(string roomId, CancellationToken token = default);

        // 4. Lấy danh sách phòng Support đang chờ (Chưa có nhân viên, IsSupport=true, IsClosed=false)
        Task<List<ChatRoom>> GetPendingSupportRoomsAsync(CancellationToken token = default);

        // 4b. Lấy danh sách phòng Support đã đóng (Phục vụ Admin/Staff xem lại)
        Task<List<ChatRoom>> GetClosedSupportRoomsAsync(int days, string? search, CancellationToken token = default);

        // 4c. Lấy TẤT CẢ phòng Support đang mở (Dành cho Admin giám sát)
        Task<List<ChatRoom>> GetActiveSupportRoomsForAdminAsync(CancellationToken token = default);

        // 5. Các hàm thao tác ghi (Create/Update)
        Task AddRoomAsync(ChatRoom room);
        Task AddMemberAsync(ChatRoomMember member);
        Task UpdateRoomAsync(ChatRoom room);
        Task<bool> SaveChangesAsync(CancellationToken token = default);
    }
}