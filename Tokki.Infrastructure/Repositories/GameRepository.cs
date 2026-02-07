using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly TokkiDbContext _dbContext;

        public GameRepository(TokkiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(IReadOnlyList<Game> Items, int TotalCount)> GetPagedForUserAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            GameType? gameType)
        {
            // Chuẩn hóa pageNumber / pageSize
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            // Base query: chỉ lấy game đang ACTIVE
            IQueryable<Game> query = _dbContext.Games
                .AsNoTracking()
                .Where(g => g.Status == GameStatus.Active);

            // Lọc theo SearchTerm (tên game)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string keyword = searchTerm.Trim();
                query = query.Where(g => g.GameName.Contains(keyword));
            }

            // Lọc theo GameType (nếu có)
            if (gameType.HasValue)
            {
                query = query.Where(g => g.GameType == gameType.Value);
            }

            // Đếm tổng số record sau khi filter
            int totalCount = await query.CountAsync();

            // Phân trang + SẮP XẾP THEO CreatedAt (mới nhất trước)
            var items = await query
                .OrderByDescending(g => g.CreatedAt)   // get theo CreatedAt
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<Game?> GetByIdAsync(string gameId)
        {
            return await _dbContext.Games
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GameId == gameId);
        }
    }
}
