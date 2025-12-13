
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Tokki.Domain.Enums;
namespace Tokki.Infrastructure.Repositories
{
    public class PassageRepository : IPassageRepository
    {
        private readonly TokkiDbContext _context;

        public PassageRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Passage?> GetByIdAsync(string passageId, CancellationToken cancellationToken = default)
        {
            return await _context.Passages
                .FirstOrDefaultAsync(p => p.PassageId == passageId, cancellationToken);
        }

        public async Task<(IEnumerable<Passage> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            PassageMediaType? mediaType = null,
            PassageStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Passages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Title!.Contains(searchTerm) || p.Content!.Contains(searchTerm));
            }

            if (mediaType.HasValue)
            {
                query = query.Where(p => p.MediaType == mediaType.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(p => p.PassageId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> IsTitleExistsAsync(string title, string? excludeId = null)
        {
            var query = _context.Passages.Where(p => p.Title == title);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(p => p.PassageId != excludeId);
            }
            return await query.AnyAsync();
        }

        public async Task AddAsync(Passage passage)
        {
            await _context.Passages.AddAsync(passage);
        }

        public Task UpdateAsync(Passage passage)
        {
            _context.Passages.Update(passage);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Passage passage)
        {
            _context.Passages.Remove(passage);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

       
    }
}
