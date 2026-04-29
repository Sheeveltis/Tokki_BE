using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class TopikLevelConfigRepository : ITopikLevelConfigRepository
    {
        private readonly TokkiDbContext _context;

        public TopikLevelConfigRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<List<TopikLevelConfig>> GetAllAsync()
        {
            return await _context.TopikLevelConfigs.ToListAsync();
        }

        public async Task<(List<TopikLevelConfig> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, int? examGroup = null, bool? isActive = null)
        {
            var query = _context.TopikLevelConfigs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.DisplayName.Contains(searchText) || x.ConfigKey.Contains(searchText));
            }

            if (examGroup.HasValue)
            {
                query = query.Where(x => x.ExamGroup == examGroup.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query.OrderBy(x => x.SortOrder)
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();
            return (items, totalCount);
        }

        public async Task<TopikLevelConfig?> GetByIdAsync(int id)
        {
            return await _context.TopikLevelConfigs.FindAsync(id);
        }

        public async Task<TopikLevelConfig?> FirstOrDefaultAsync(Expression<Func<TopikLevelConfig, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.TopikLevelConfigs.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task AddAsync(TopikLevelConfig config)
        {
            await _context.TopikLevelConfigs.AddAsync(config);
        }

        public void Update(TopikLevelConfig config)
        {
            _context.TopikLevelConfigs.Update(config);
        }

        public void Delete(TopikLevelConfig config)
        {
            _context.TopikLevelConfigs.Remove(config);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
