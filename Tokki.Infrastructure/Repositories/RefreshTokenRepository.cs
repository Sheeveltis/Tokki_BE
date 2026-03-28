// Infrastructure/Repositories/RefreshTokenRepository.cs
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly TokkiDbContext _context; // ← đổi đúng tên DbContext của bạn

        public RefreshTokenRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string hash)
        {
            return await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash);
        }

        public async Task<List<RefreshToken>> GetAllByUserIdAsync(string userId)
        {
            return await _context.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public async Task DeleteExpiredAsync(DateTime now)
        {
            await _context.RefreshTokens
                .Where(t => t.ExpiryDate < now)
                .ExecuteDeleteAsync();
        }

        public async Task DeleteRevokedAndExpiredAsync(DateTime now)
        {
            await _context.RefreshTokens
                .Where(t => t.Revoked && t.ExpiryDate < now)
                .ExecuteDeleteAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}