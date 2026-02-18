using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class MiniGameRepository : IMiniGameRepository
    {
        private readonly TokkiDbContext _context;

        public MiniGameRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vocabulary>> GetRandomVocabulariesByTopicAsync(string topicId, int quantity, CancellationToken cancellationToken)
        {
            return await _context.VocabularyTopics 
                .AsNoTracking()
                .Where(vt => vt.TopicId == topicId &&
                             vt.Status == Domain.Enums.VocabularyTopicStatus.Active)
                .Select(vt => vt.Vocabulary)
                .Where(v => v.Status == Domain.Enums.VocabularyStatus.Active)
                .OrderBy(v => Guid.NewGuid())
                .Take(quantity)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<Topic>> GetSolitaireTopicsWithVocabsAsync(CancellationToken token = default)
        {
            return await _context.Topics
                .AsNoTracking()
                .Where(t => t.Status == TopicStatus.Active && t.TopicType == TopicType.Solitaire)
                .Include(t => t.VocabularyTopics.Where(vt => vt.Status == VocabularyTopicStatus.Active))
                     .ThenInclude(vt => vt.Vocabulary)
                .OrderBy(t => Guid.NewGuid())
                .Take(50)
                .ToListAsync(token);
        }
        public async Task<List<DailyWordle>> GetDailyWordlesByDateAsync(DateOnly date, CancellationToken token = default)
        {
            return await _context.DailyWordles
                .Where(x => x.GameDate == date)
                .OrderBy(x => x.Level) 
                .ToListAsync(token);
        }

        public async Task<List<UserWordleProgress>> GetUserWordleProgressAsync(string userId, IEnumerable<string> dailyWordleIds, CancellationToken token = default)
        {
            if (dailyWordleIds == null || !dailyWordleIds.Any())
            {
                return new List<UserWordleProgress>();
            }

            return await _context.UserWordleProgress
                .Where(x => x.UserId == userId && dailyWordleIds.Contains(x.DailyWordleId))
                .ToListAsync(token);
        }
        public async Task<DailyWordle?> GetDailyWordleByIdAsync(string id, CancellationToken token = default)
        {
            return await _context.DailyWordles.FindAsync(new object[] { id }, token);
        }

        public void AddUserWordleProgress(UserWordleProgress progress)
        {
            _context.UserWordleProgress.Add(progress);
        }

        public async Task<int> SaveChangesAsync(CancellationToken token = default)
        {
            return await _context.SaveChangesAsync(token);
        }
    }
}