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
                .Where(t => t.Status == TopicStatus.Active
                         && t.TopicType == TopicType.Solitaire
                         && t.VocabularyTopics.Count(vt => vt.Status == VocabularyTopicStatus.Active && vt.Vocabulary != null) >= 4)
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
        public async Task<WordleSentenceSubmission?> GetWordleSubmissionByIdAsync(string submissionId, CancellationToken token)
        {
            return await _context.WordleSentenceSubmissions
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, token);
        }
        public async Task<List<WordleSentenceSubmission>> GetTopPublicSentencesAsync(string dailyWordleId, int top, CancellationToken token)
        {
            return await _context.WordleSentenceSubmissions
                .Include(s => s.User)
                    .ThenInclude(u => u.CurrentTitle) 
                .Include(s => s.SentenceLikes)
                .Where(s => s.DailyWordleId == dailyWordleId && s.IsPublic)
                .OrderByDescending(s => s.AiScore)
                .ThenByDescending(s => s.LikeCount)
                .ThenByDescending(s => s.CreatedAt)
                .Take(top)
                .AsNoTracking()
                .ToListAsync(token);
        }
        public async Task<WordleSentenceLike?> GetLikeAsync(string userId, string submissionId, CancellationToken token)
        {
            return await _context.WordleSentenceLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.SubmissionId == submissionId, token);
        }

        public void AddLike(WordleSentenceLike like) => _context.WordleSentenceLikes.Add(like);

        public void RemoveLike(WordleSentenceLike like) => _context.WordleSentenceLikes.Remove(like);

        public async Task<(List<UserWordleProgress> Items, int TotalCount)> GetWordlePlayersAsync(string dailyWordleId, int pageIndex, int pageSize, CancellationToken token)
        {
            var query = _context.UserWordleProgress
                .Include(p => p.User)
                .Where(p => p.DailyWordleId == dailyWordleId);

            int totalCount = await query.CountAsync(token);

            var items = await query
                .OrderByDescending(p => p.IsWon)
                .ThenBy(p => p.AttemptCount)
                .ThenBy(p => p.LastActivity)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return (items, totalCount);
        }

        public async Task<(List<WordleSentenceSubmission> Items, int TotalCount)> GetWordleLeaderboardAsync(string dailyWordleId, int pageIndex, int pageSize, CancellationToken token, bool includePrivate = false)
        {
            var query = _context.WordleSentenceSubmissions
                .Include(s => s.User)
                    .ThenInclude(u => u.CurrentTitle)
                .Include(s => s.SentenceLikes)
                .Where(s => s.DailyWordleId == dailyWordleId);

            if (!includePrivate)
            {
                query = query.Where(s => s.IsPublic);
            }

            int totalCount = await query.CountAsync(token);

            var items = await query
                .OrderByDescending(s => s.AiScore)
                .ThenByDescending(s => s.LikeCount)
                .ThenByDescending(s => s.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return (items, totalCount);
        }
    }
}