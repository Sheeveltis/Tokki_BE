using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserPronunciationProgressRepository : IUserPronunciationProgressRepository
    {
        private readonly TokkiDbContext _context;

        public UserPronunciationProgressRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserPronunciationProgress?> GetByUserIdAndRuleIdAsync(string userId, string ruleId)
        {
            return await _context.UserPronunciationProgresses
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PronunciationRuleId == ruleId);
        }

        public async Task<List<UserPronunciationProgress>> GetByUserIdAndRuleIdsAsync(string userId, List<string> ruleIds)
        {
            return await _context.UserPronunciationProgresses
                .AsNoTracking()
                .Where(x => x.UserId == userId && ruleIds.Contains(x.PronunciationRuleId))
                .ToListAsync();
        }

        public async Task AddAsync(UserPronunciationProgress progress)
        {
            await _context.UserPronunciationProgresses.AddAsync(progress);
        }

        public void Update(UserPronunciationProgress progress)
        {
            _context.UserPronunciationProgresses.Update(progress);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
