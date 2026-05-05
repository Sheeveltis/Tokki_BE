using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class AlphabetRepository : IAlphabetRepository
    {
        private readonly TokkiDbContext _context;

        public AlphabetRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<List<AlphabetData>> GetAllAsync()
        {
            return await _context.AlphabetData.OrderBy(x => x.SortOrder).ToListAsync();
        }

        public async Task<AlphabetData?> GetByIdAsync(int id)
        {
            return await _context.AlphabetData.FindAsync(id);
        }

        public async Task<AlphabetData?> GetByLetterAsync(string letter)
        {
            return await _context.AlphabetData.FirstOrDefaultAsync(x => x.Letter == letter);
        }

        public async Task AddAsync(AlphabetData entity)
        {
            await _context.AlphabetData.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<AlphabetData> entities)
        {
            await _context.AlphabetData.AddRangeAsync(entities);
        }

        public async Task UpdateAsync(AlphabetData entity)
        {
            _context.AlphabetData.Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(AlphabetData entity)
        {
            _context.AlphabetData.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task<(List<AlphabetData> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            AlphabetType? type = null,
            bool? isActive = null)
        {
            var query = _context.AlphabetData.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.Letter.Contains(searchTerm) || (x.Meaning != null && x.Meaning.Contains(searchTerm)));
            }

            if (type.HasValue)
            {
                query = query.Where(x => x.Type == type.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.SortOrder)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
