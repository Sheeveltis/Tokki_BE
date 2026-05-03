using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly TokkiDbContext _context;

        public ChatRoomRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRoom?> GetPrivateRoomAsync(string user1Id, string user2Id, CancellationToken token = default)
        {
            return await _context.ChatRooms
                .Where(r => !r.IsGroup && !r.IsSupport)
                .Where(r => r.Members.Any(m => m.UserId == user1Id) &&
                            r.Members.Any(m => m.UserId == user2Id))
                .FirstOrDefaultAsync(token);
        }

        public async Task<List<ChatRoom>> GetUserRoomsAsync(string userId, CancellationToken token = default)
        {
            return await _context.ChatRooms
                .AsNoTracking()
                .Where(r => r.Members.Any(m => m.UserId == userId) && !r.IsClosed) 
                .Include(r => r.Members) 
                .ThenInclude(m => m.User)
                .OrderByDescending(r => r.CreatedAt) 
                .ToListAsync(token);
        }

        public async Task<ChatRoom?> GetRoomByIdAsync(string roomId, CancellationToken token = default)
        {
            return await _context.ChatRooms
                .Include(r => r.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(r => r.ChatRoomId == roomId, token);
        }

        public async Task<List<ChatRoom>> GetPendingSupportRoomsAsync(CancellationToken token = default)
        {
            return await _context.ChatRooms
                .Include(r => r.Members)
                .ThenInclude(m => m.User)
                .Where(r => r.IsSupport
                            && !r.IsClosed
                            && r.Members.Count == 1)
                .OrderBy(r => r.CreatedAt) 
                .ToListAsync(token);
        }

        public async Task<List<ChatRoom>> GetClosedSupportRoomsAsync(int days, string? search, CancellationToken token = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var query = _context.ChatRooms
                .Include(r => r.Members)
                .ThenInclude(m => m.User)
                .Where(r => r.IsSupport && r.IsClosed && r.CreatedAt >= cutoffDate);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Members.Any(m => m.User.FullName.Contains(search)));
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(token);
        }

        public async Task<List<ChatRoom>> GetActiveSupportRoomsForAdminAsync(CancellationToken token = default)
        {
            return await _context.ChatRooms
                .Include(r => r.Members)
                .ThenInclude(m => m.User)
                .Where(r => r.IsSupport && !r.IsClosed)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(token);
        }

        public async Task AddRoomAsync(ChatRoom room)
        {
            await _context.ChatRooms.AddAsync(room);
        }

        public async Task AddMemberAsync(ChatRoomMember member)
        {
            await _context.ChatRoomMembers.AddAsync(member);
        }

        public async Task UpdateRoomAsync(ChatRoom room)
        {
            _context.ChatRooms.Update(room);
        }
        public async Task<bool> SaveChangesAsync(CancellationToken token = default)
        {
            return await _context.SaveChangesAsync(token) > 0;
        }
    }
}