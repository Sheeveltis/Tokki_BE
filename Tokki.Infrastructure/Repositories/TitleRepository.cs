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

        public async Task<List<Title>> GetAllTitlesAsync()
        {
            return await _context.Titles
                .Where(t => t.Status == TitleStatus.Active) 
                .OrderBy(t => t.RequiredXP)
                .ToListAsync();
        }

        public async Task<Title?> GetTitleByNameAsync(string name)
        {
            return await _context.Titles.FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<Title?> GetTitleByIdAsync(int id)
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
    }
}