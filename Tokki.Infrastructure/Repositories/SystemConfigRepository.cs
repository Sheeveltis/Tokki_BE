using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly TokkiDbContext _context;

        public SystemConfigRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key)
        {
            return await _context.SystemConfig.FirstOrDefaultAsync(x => x.Key == key);
        }

        public async Task<List<SystemConfig>> GetAllAsync()
        {
            return await _context.SystemConfig.ToListAsync();
        }

        public async Task AddAsync(SystemConfig config)
        {
            await _context.SystemConfig.AddAsync(config);
        }
 
        public async Task AddRangeAsync(IEnumerable<SystemConfig> configs)
        {
            await _context.SystemConfig.AddRangeAsync(configs);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<(List<SystemConfig> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, Tokki.Domain.Enums.SystemConfigType? configType = null, string? searchTerm = null, bool? isActive = null)
        {
            var query = _context.SystemConfig.AsNoTracking();
            if (configType.HasValue)
            {
                query = query.Where(x => x.ConfigType == configType.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.Key.Contains(searchTerm) 
                                      || (x.Value != null && x.Value.Contains(searchTerm))
                                      || (x.Description != null && x.Description.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<string?> GetValueByKeyAsync(string key)
        {
            var value = await _context.SystemConfig
                                      .AsNoTracking()
                                      .Where(x => x.Key == key && x.IsActive) // Nên check thêm IsActive
                                      .Select(x => x.Value)
                                      .FirstOrDefaultAsync();
            return value;
        }
        public async Task<SystemConfig?> FirstOrDefaultAsync(Expression<Func<SystemConfig, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.SystemConfig.FirstOrDefaultAsync(predicate, cancellationToken);
        }
    }
}