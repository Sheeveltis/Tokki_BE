using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tokki.Infrastructure.Repositories
{
    public class UserFavoriteWordRepository : IUserFavoriteWordRepository
    {
        private readonly TokkiDbContext _context;

      
        public UserFavoriteWordRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserFavoriteWord?> GetByIdAsync(string favoriteWordId)
        {
            return await _context.UserFavoriteWords
                .Include(f => f.Word)
                .Include(f => f.Meaning)
                .FirstOrDefaultAsync(f => f.FavoriteWordId == favoriteWordId);
        }

        public async Task<UserFavoriteWord?> GetByUserAndWordAsync(string userId, string wordId)
        {
            return await _context.UserFavoriteWords
                .Include(f => f.Word)
                .Include(f => f.Meaning)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.WordId == wordId);
        }

        public async Task<(List<UserFavoriteWord> Items, int TotalCount)> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            UserFavoriteWordStatus? status = null)
        {
            var query = _context.UserFavoriteWords
                .Include(f => f.Word)
                .Include(f => f.Meaning)
                .Where(f => f.UserId == userId);

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
            }

            // Search by word text
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(f => f.Word.Text.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(f => f.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<UserFavoriteWord> AddAsync(UserFavoriteWord favoriteWord)
        {
            await _context.UserFavoriteWords.AddAsync(favoriteWord);
            return favoriteWord;
        }

        public Task UpdateAsync(UserFavoriteWord favoriteWord)
        {
            _context.UserFavoriteWords.Update(favoriteWord);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(UserFavoriteWord favoriteWord)
        {
            _context.UserFavoriteWords.Remove(favoriteWord);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
