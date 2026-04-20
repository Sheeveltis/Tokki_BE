using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Tokki.Infrastructure.Repositories
{
    public class PronunciationRuleRepository : IPronunciationRuleRepository
    {
        private readonly TokkiDbContext _context;
        public PronunciationRuleRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PronunciationRule rule)
        {
            await _context.PronunciationRules.AddAsync(rule);
        }

        public async Task AddRangeAsync(IEnumerable<PronunciationRule> rules)
        {
            await _context.PronunciationRules.AddRangeAsync(rules);
        }

        public Task UpdateAsync(PronunciationRule rule)
        {
            _context.PronunciationRules.Update(rule);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PronunciationRule rule)
        {
            rule.IsDeleted = true;
            _context.PronunciationRules.Update(rule);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PronunciationRule?> GetByIdAsync(string id)
        {
            return await _context.PronunciationRules
                .FirstOrDefaultAsync(r => r.PronunciationRuleId == id && !r.IsDeleted);
        }

        public async Task<PronunciationRule?> GetByIdWithDetailsAsync(string id)
        {
            return await _context.PronunciationRules
                .Include(r => r.Examples)
                .Include(r => r.Creator)
                .Include(r => r.Updater)
                .FirstOrDefaultAsync(r => r.PronunciationRuleId == id && !r.IsDeleted);
        }

        public async Task<bool> IsRuleNameExistsAsync(string ruleName, string? excludeId = null)
        {
            var query = _context.PronunciationRules.Where(r => r.RuleName == ruleName && !r.IsDeleted);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(r => r.PronunciationRuleId != excludeId);
            }
            return await query.AnyAsync();
        }

        public async Task<List<PronunciationRule>> GetAllActiveRulesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PronunciationRules
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<PronunciationRule> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query = _context.PronunciationRules.AsNoTracking().Where(r => !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim();
                query = query.Where(r => r.RuleName.Contains(search) 
                                     || (r.Description != null && r.Description.Contains(search))
                                     || r.PronunciationRuleId.Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(r => r.SortOrder)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken = default)
        {
            var max = await _context.PronunciationRules
                .Where(x => !x.IsDeleted)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);
            return max ?? 0;
        }
    }
}
