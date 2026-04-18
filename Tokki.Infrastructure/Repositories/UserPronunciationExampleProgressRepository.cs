using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserPronunciationExampleProgressRepository : IUserPronunciationExampleProgressRepository
    {
        private readonly TokkiDbContext _context;

        public UserPronunciationExampleProgressRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserPronunciationExampleProgress?> GetByUserIdAndExampleIdAsync(string userId, string exampleId)
        {
            return await _context.UserPronunciationExampleProgresses
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PronunciationExampleId == exampleId);
        }

        public async Task<List<UserPronunciationExampleProgress>> GetByUserIdAndRuleIdAsync(string userId, string ruleId)
        {
            return await (from ep in _context.UserPronunciationExampleProgresses
                          join e in _context.PronunciationExamples on ep.PronunciationExampleId equals e.ExampleId
                          where ep.UserId == userId && e.PronunciationRuleId == ruleId
                          select ep).ToListAsync();
        }

        public async Task<int> CountPracticedByUserIdAndRuleIdAsync(string userId, string ruleId)
        {
            return await (from ep in _context.UserPronunciationExampleProgresses
                          join e in _context.PronunciationExamples on ep.PronunciationExampleId equals e.ExampleId
                          where ep.UserId == userId && e.PronunciationRuleId == ruleId && ep.IsPracticed
                          select ep).CountAsync();
        }

        public async Task AddAsync(UserPronunciationExampleProgress progress)
        {
            await _context.UserPronunciationExampleProgresses.AddAsync(progress);
        }

        public void Update(UserPronunciationExampleProgress progress)
        {
            _context.UserPronunciationExampleProgresses.Update(progress);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
