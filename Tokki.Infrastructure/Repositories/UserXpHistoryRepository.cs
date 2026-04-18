using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

using Tokki.Domain.Enums;
namespace Tokki.Infrastructure.Repositories
{
    public class UserXpHistoryRepository : IUserXpHistoryRepository
    {
        private readonly TokkiDbContext _context;

        public UserXpHistoryRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserXpHistory history)
        {
            await _context.UserXpHistories.AddAsync(history);
        }

        public async Task<long> GetTotalXpBySourceAndDateAsync(string userId, XpSource action, DateTime date)
        {
            return await _context.UserXpHistories
                .Where(h => h.UserId == userId && h.Action == action && h.CreatedAt.Date == date.Date)
                .SumAsync(h => (long?)h.Amount) ?? 0;
        }

        public async Task<int> CountActiveDaysAsync(string userId)
        {
            return await _context.UserXpHistories
                .Where(h => h.UserId == userId)
                .Select(h => h.CreatedAt.Date)
                .Distinct()
                .CountAsync();
        }
    }
}
