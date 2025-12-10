using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class TitleRepository : ITitleRepository
    {
        private readonly TokkiDbContext _context;

        public TitleRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Title>> GetAllTitlesAsync(bool includeInactive = false)
        {
            var query = _context.Titles.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(t => t.Status == TitleStatus.Active);
            }

            return await query.OrderBy(t => t.RequiredXP).ToListAsync();
        }

        public async Task<Title?> GetTitleByNameAsync(string name)
        {
            return await _context.Titles.FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<Title?> GetTitleByIdAsync(string id)
        {
            return await _context.Titles.FindAsync(id);
        }

        public async Task<Title?> GetTitleByXpAsync(long xp)
        {
            return await _context.Titles
                .Where(t => !t.IsSystemGiven
                            && t.RequiredXP <= xp
                            && t.Status == TitleStatus.Active) 
                .OrderByDescending(t => t.RequiredXP)
                .FirstOrDefaultAsync();
        }
        public async Task AddAsync(Title title)
        {
            await _context.Titles.AddAsync(title);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Title title)
        {
            _context.Titles.Update(title);
            await _context.SaveChangesAsync();
        }
    }
}