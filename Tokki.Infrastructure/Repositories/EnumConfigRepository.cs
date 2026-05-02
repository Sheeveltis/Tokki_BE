using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class EnumConfigRepository : IEnumConfigRepository
    {
        private readonly TokkiDbContext _context;

        public EnumConfigRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<List<EnumConfig>> GetByGroupAsync(EnumGroup groupCode)
        {
            return await _context.EnumConfigs
                .AsNoTracking()
                .Where(x => x.GroupCode == groupCode && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        public async Task<(List<EnumConfig> Items, int TotalCount)> GetByGroupPagedAsync(EnumGroup groupCode, int pageNumber, int pageSize)
        {
            var query = _context.EnumConfigs
                .AsNoTracking()
                .Where(x => x.GroupCode == groupCode && x.IsActive);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.SortOrder)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<EnumConfig?> GetByValueAsync(EnumGroup groupCode, int value)
        {
            return await _context.EnumConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GroupCode == groupCode && x.Value == value);
        }

        public async Task<EnumConfig?> GetByKeyAsync(EnumGroup groupCode, string key)
        {
            return await _context.EnumConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GroupCode == groupCode && x.Key == key);
        }

        public async Task<List<EnumConfig>> GetAllAsync()
        {
            return await _context.EnumConfigs
                .AsNoTracking()
                .OrderBy(x => x.GroupCode)
                .ThenBy(x => x.SortOrder)
                .ToListAsync();
        }

        public async Task<List<EnumConfig>> GetFilteredAsync(EnumGroup? groupCode)
        {
            var query = _context.EnumConfigs.AsNoTracking();
            if (groupCode.HasValue)
            {
                query = query.Where(x => x.GroupCode == groupCode.Value);
            }
            return await query.OrderBy(x => x.GroupCode).ThenBy(x => x.SortOrder).ToListAsync();
        }

        public async Task<EnumConfig?> FirstOrDefaultAsync(Expression<Func<EnumConfig, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.EnumConfigs.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task AddAsync(EnumConfig config)
        {
            await _context.EnumConfigs.AddAsync(config);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
