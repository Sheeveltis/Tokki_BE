using Microsoft.EntityFrameworkCore;
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

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<(List<SystemConfig> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }
    }
}