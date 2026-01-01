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
            string gameId,
            string topicId,
            GameDifficulty difficulty)
        {
            return await _dbContext.GameMatchSessions
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId &&
                    s.GameId == gameId &&
                    s.TopicId == topicId &&
                    s.GameDifficulty == difficulty);
        }

        public async Task<(IReadOnlyList<GameMatchSession> Items, int TotalCount)> GetPagedByGameTopicAsync(
            string gameId,
            string topicId,
            GameDifficulty difficulty,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _dbContext.GameMatchSessions
                .AsNoTracking()
                .Where(s =>
                    s.GameId == gameId &&
                    s.TopicId == topicId &&
                    s.GameDifficulty == difficulty);

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
