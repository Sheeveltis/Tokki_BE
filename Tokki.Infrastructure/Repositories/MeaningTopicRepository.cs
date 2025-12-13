using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class MeaningTopicRepository : IMeaningTopicRepository
    {
        private readonly TokkiDbContext _context;

        public MeaningTopicRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<MeaningTopic?> GetByIdAsync(string meaningId, string topicId)
        {
            return await _context.MeaningTopic
                .Include(mt => mt.Meaning)
                .Include(mt => mt.Topic)
                .FirstOrDefaultAsync(mt =>
                    mt.MeaningId == meaningId &&
                    mt.TopicId == topicId);
        }

        public async Task<List<MeaningTopic>> GetByMeaningIdAsync(string meaningId)
        {
            return await _context.MeaningTopic
                .Include(mt => mt.Topic)
                .Where(mt => mt.MeaningId == meaningId)
                .ToListAsync();
        }

        public async Task<List<MeaningTopic>> GetByTopicIdAsync(string topicId)
        {
            return await _context.MeaningTopic
                .Include(mt => mt.Meaning)
                    .ThenInclude(m => m.Word)
                .Where(mt => mt.TopicId == topicId)
                .ToListAsync();
        }

        public async Task<List<MeaningTopic>> GetByMeaningIdsAsync(List<string> meaningIds)
        {
            return await _context.MeaningTopic
                .Include(mt => mt.Topic)
                .Where(mt => meaningIds.Contains(mt.MeaningId))
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string meaningId, string topicId)
        {
            return await _context.MeaningTopic 
                .AnyAsync(mt => mt.MeaningId == meaningId && mt.TopicId == topicId);
        }

        public async Task AddAsync(MeaningTopic meaningTopic)
        {
            await _context.MeaningTopic.AddAsync(meaningTopic);
        }

        public async Task DeleteAsync(MeaningTopic meaningTopic)
        {
            _context.MeaningTopic.Remove(meaningTopic);
            await Task.CompletedTask;
        }

        public async Task DeleteRangeByMeaningIdAsync(string meaningId)
        {
            var meaningTopic = await _context.MeaningTopic
                .Where(mt => mt.MeaningId == meaningId)
                .ToListAsync();

            _context.MeaningTopic.RemoveRange(meaningTopic);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<List<Meaning>> GetMeaningsByWordIdAndTopicIdAsync(string wordId, string topicId)
        {
            return await _context.Meaning
                .Include(m => m.MeaningTopics)
                .Where(m => m.WordId == wordId &&
                            m.MeaningTopics.Any(mt => mt.TopicId == topicId &&
                                                     mt.Status == MeaningTopicStatus.Active))
                .OrderByDescending(m => m.CreateDate)
                .ToListAsync();
        }
    }
}
