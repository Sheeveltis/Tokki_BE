using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
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
                .Where(t => t.IsSystemGiven == false && t.RequiredXP <= xp)
                .OrderByDescending(t => t.RequiredXP)
                .FirstOrDefaultAsync();
        }
    }
}