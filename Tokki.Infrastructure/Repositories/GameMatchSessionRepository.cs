using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class GameMatchSessionRepository : IGameMatchSessionRepository
    {
        private readonly TokkiDbContext _dbContext;

        public GameMatchSessionRepository(TokkiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GameMatchSession?> GetByUserGameTopicAsync(
            string userId,
            GameType gameType,
            string? topicId,
            GameDifficulty difficulty)
        {
            return await _dbContext.GameMatchSessions
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId &&
                    s.GameType == gameType &&
                    s.TopicId == topicId &&
                    s.GameDifficulty == difficulty);
        }

        public async Task<(IReadOnlyList<GameMatchSession> Items, int TotalCount)> GetAllByUserAsync(
            string userId,
            GameType? gameType,
            string? topicId,
            GameDifficulty? difficulty,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _dbContext.GameMatchSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId);

            if (gameType.HasValue)
            {
                query = query.Where(s => s.GameType == gameType.Value);
            }

            if (!string.IsNullOrEmpty(topicId))
            {
                query = query.Where(s => s.TopicId == topicId);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(s => s.GameDifficulty == difficulty.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.GameType)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IReadOnlyList<GameMatchSession> Items, int TotalCount)> GetPagedByGameTopicAsync(
            GameType? gameType,
            string? topicId,
            GameDifficulty? difficulty,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _dbContext.GameMatchSessions
                .AsNoTracking()
                .AsQueryable();

            if (gameType.HasValue)
            {
                query = query.Where(s => s.GameType == gameType.Value);
            }

            if (!string.IsNullOrEmpty(topicId))
            {
                query = query.Where(s => s.TopicId == topicId);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(s => s.GameDifficulty == difficulty.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(s => s.BestScore)
                .ThenByDescending(s => s.LatestScore)
                .ThenByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IReadOnlyList<(string UserId, GameType GameType, GameDifficulty GameDifficulty, string? TopicId, int BestScore)> Items, int TotalCount)> GetLeaderboardAsync(
            GameType? gameType,
            GameDifficulty? difficulty,
            string? topicId,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _dbContext.GameMatchSessions.AsNoTracking().AsQueryable();

            if (gameType.HasValue)
            {
                query = query.Where(s => s.GameType == gameType.Value);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(s => s.GameDifficulty == difficulty.Value);
            }

            if (!string.IsNullOrEmpty(topicId))
            {
                query = query.Where(s => s.TopicId == topicId);
            }

            var groupedQuery = query.GroupBy(s => new { 
                s.UserId, 
                s.GameType, 
                s.GameDifficulty, 
                TopicId = string.IsNullOrEmpty(s.TopicId) ? null : s.TopicId 
            })
                .Select(g => new { 
                    UserId = g.Key.UserId, 
                    GameType = g.Key.GameType,
                    GameDifficulty = g.Key.GameDifficulty,
                    TopicId = g.Key.TopicId,
                    BestScore = g.Max(x => x.BestScore) 
                });

            var totalCount = await groupedQuery.CountAsync();

            var dbItems = await groupedQuery
                .OrderByDescending(x => x.BestScore)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = dbItems.Select(x => (x.UserId, x.GameType, x.GameDifficulty, x.TopicId, x.BestScore)).ToList();

            return (items, totalCount);
        }

        public async Task AddAsync(GameMatchSession session)
        {
            await _dbContext.GameMatchSessions.AddAsync(session);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
