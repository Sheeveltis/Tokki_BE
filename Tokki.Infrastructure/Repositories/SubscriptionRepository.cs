using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly TokkiDbContext _context;

        public SubscriptionRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Subscription subscription)
        {
            await _context.Subscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Subscription>> GetByUserIdAsync(string userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}