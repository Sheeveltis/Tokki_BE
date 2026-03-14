using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserWeaknessRepository : IUserWeaknessRepository
    {
        private readonly TokkiDbContext _context;

        public UserWeaknessRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserWeakness entity, CancellationToken cancellationToken = default)
        {
            await _context.UserWeaknesses.AddAsync(entity, cancellationToken);
        }

        public async Task<List<UserWeakness>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserWeaknesses
                .Where(w => w.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}