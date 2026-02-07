using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums; 
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserRoadmapRepository : IUserRoadmapRepository
    {
        private readonly TokkiDbContext _context;

        public UserRoadmapRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserRoadmap roadmap)
        {
            await _context.UserRoadmaps.AddAsync(roadmap);
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<UserRoadmap?> GetActiveRoadmapByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoadmaps
                .Include(r => r.Weeks)
                    .ThenInclude(w => w.DailyTasks)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CurrentStatus == UserRoadmapStatus.Active, cancellationToken);
        }
    }
}