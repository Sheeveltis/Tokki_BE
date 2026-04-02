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

            return await query.OrderBy(t => t.RequirementType).ThenBy(t => t.RequirementQuantity).ToListAsync();
        }

        public async Task<Title?> GetTitleByNameAsync(string name, TitleStatus? status = null)
        {
            var query = _context.Titles.AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }
            return await query.FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<Title?> GetTitleByIdAsync(string id)
        {
            return await _context.Titles.FindAsync(id);
        }

        public async Task<List<Title>> GetEligibleTitlesAsync(TitleRequirementType type, long quantity)
        {
            var query = _context.Titles.AsNoTracking()
                .Where(t => t.Status == TitleStatus.Active && t.RequirementType == type);

            // Level/XP/Streak: quantity >= threshold
            return await query.Where(t => t.RequirementQuantity <= quantity).ToListAsync();
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

        public async Task<(List<Title> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            TitleStatus? status = null,
            TitleRequirementType? requirementType = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Titles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(t => t.TitleId.ToLower().Contains(term) || t.Name.ToLower().Contains(term));
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (requirementType.HasValue)
            {
                query = query.Where(t => t.RequirementType == requirementType.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(t => t.RequirementType)
                .ThenBy(t => t.RequirementQuantity)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}