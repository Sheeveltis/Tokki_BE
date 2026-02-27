using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class PronunciationExampleRepository : IPronunciationExampleRepository
    {
        private readonly TokkiDbContext _context;

        public PronunciationExampleRepository(TokkiDbContext context)
        {
            _context = context;
        }
        public async Task<PronunciationExample?> GetByIdAsync(string id)
        {
            return await _context.PronunciationExamples
                .FirstOrDefaultAsync(x => x.ExampleId == id && !x.IsDeleted);
        }

        public async Task AddRangeAsync(IEnumerable<PronunciationExample> entities)
        {
            await _context.PronunciationExamples.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
        public async Task<List<PronunciationExample>> GetExamplesByRuleIdAsync(string ruleId, CancellationToken cancellationToken = default)
        {
            return await _context.PronunciationExamples
                .Where(e => e.PronunciationRuleId == ruleId && !e.IsDeleted)
                .OrderBy(e => e.SortOrder)
                .ToListAsync(cancellationToken);
        }

        public async Task<PronunciationExample?> GetDetailByIdAsync(string exampleId, CancellationToken cancellationToken = default)
        {
            return await _context.PronunciationExamples
                .Include(e => e.PronunciationRule)
                .FirstOrDefaultAsync(e => e.ExampleId == exampleId && !e.IsDeleted, cancellationToken);
        }
    }
}
