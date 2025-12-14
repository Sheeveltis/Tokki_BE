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
    public class VocabularyTopicRepository : IVocabularyTopicRepository
    {
        private readonly TokkiDbContext _context;

        public VocabularyTopicRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<VocabularyTopic?> GetByVocabularyAndTopicAsync(string vocabularyId, string topicId)
        {
            return await _context.VocabularyTopics
                .Include(vt => vt.Vocabulary)
                .Include(vt => vt.Topic)
                .FirstOrDefaultAsync(vt => 
                    vt.VocabularyId == vocabularyId && 
                    vt.TopicId == topicId);
        }

        public async Task<List<VocabularyTopic>> GetByVocabularyIdAsync(string vocabularyId)
        {
            return await _context.VocabularyTopics
                .Include(vt => vt.Topic)
                .Where(vt => vt.VocabularyId == vocabularyId)
                .ToListAsync();
        }

        public async Task<List<VocabularyTopic>> GetByTopicIdAsync(string topicId)
        {
            return await _context.VocabularyTopics
                .Include(vt => vt.Vocabulary)
                .Where(vt => vt.TopicId == topicId)
                .ToListAsync();
        }

        public async Task AddAsync(VocabularyTopic vocabularyTopic)
        {
            await _context.VocabularyTopics.AddAsync(vocabularyTopic);
        }

        public async Task UpdateAsync(VocabularyTopic vocabularyTopic)
        {
            _context.VocabularyTopics.Update(vocabularyTopic);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(VocabularyTopic vocabularyTopic)
        {
            _context.VocabularyTopics.Remove(vocabularyTopic);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
