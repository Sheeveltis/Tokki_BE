using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class VipPackageRepository : IVipPackageRepository
    {
        private readonly TokkiDbContext _context;

        public VipPackageRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(VipPackage package)
        {
            await _context.VipPackages.AddAsync(package);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(VipPackage package)
        {
            _context.VipPackages.Update(package);
            await _context.SaveChangesAsync();
        }

        public async Task<VipPackage?> GetByIdAsync(string id)
        {
            return await _context.VipPackages
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<List<VipPackage>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.VipPackages.AsQueryable();

            query = query.Where(p => !p.IsDeleted);

            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            return await query.OrderBy(p => p.Price).ToListAsync();
        }
    }
}