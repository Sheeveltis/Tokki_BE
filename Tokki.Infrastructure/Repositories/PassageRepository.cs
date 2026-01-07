using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

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
            if (string.IsNullOrWhiteSpace(passageId))
                return null;

            var id = passageId.Trim();

            return await _context.Passages
                .FirstOrDefaultAsync(p => p.PassageId == id, cancellationToken);
        }

        public async Task<(IEnumerable<Passage> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            PassageMediaType? mediaType = null,
            PassageStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Passages.AsQueryable();

            // Search (null-safe)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p =>
                    (p.Title != null && p.Title.Contains(term)) ||
                    (p.Content != null && p.Content.Contains(term)));
            }

            // Filter MediaType
            if (mediaType.HasValue)
            {
                query = query.Where(p => p.MediaType == mediaType.Value);
            }

            // ✅ Không filter Status mặc định
            // Chỉ lọc khi client truyền status
            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)   
                .ThenByDescending(p => p.PassageId)   
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> IsTitleExistsAsync(string title, string? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            var t = title.Trim();

            var query = _context.Passages.AsQueryable();

            query = query.Where(p => p.Title != null && p.Title.Trim() == t);

            if (!string.IsNullOrWhiteSpace(excludeId))
            {
                var exId = excludeId.Trim();
                query = query.Where(p => p.PassageId != exId);
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

        // Giữ nguyên hard delete (nếu bạn muốn soft delete thì xử lý ở handler)
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
