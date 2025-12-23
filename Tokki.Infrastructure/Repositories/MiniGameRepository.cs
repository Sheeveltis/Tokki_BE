using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
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
    }
}