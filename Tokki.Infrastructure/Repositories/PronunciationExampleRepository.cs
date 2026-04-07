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

        public async Task AddAsync(PronunciationExample entity)
        {
            await _context.PronunciationExamples.AddAsync(entity);
        }

        public Task UpdateAsync(PronunciationExample entity)
        {
            _context.PronunciationExamples.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PronunciationExample entity)
        {
            entity.IsDeleted = true;
            _context.PronunciationExamples.Update(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
 
        public async Task<List<PronunciationExample>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PronunciationExamples
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.PronunciationRuleId)
                .ThenBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);
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

        public async Task<(List<PronunciationExample> Items, int TotalCount)> GetPagedAsync(
            string ruleId,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query = _context.PronunciationExamples
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.PronunciationRuleId == ruleId);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(e => e.TargetScript.Contains(searchTerm) 
                    || e.RawScript.Contains(searchTerm) 
                    || (e.Meaning != null && e.Meaning.Contains(searchTerm)));
            }

            int totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(e => e.SortOrder) // Always sort by SortOrder as requested
                .ThenByDescending(e => e.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
