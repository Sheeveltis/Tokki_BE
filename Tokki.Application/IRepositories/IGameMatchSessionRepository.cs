using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IGameMatchSessionRepository
    {
        Task<GameMatchSession?> GetByUserGameTopicAsync(
            string userId,
            GameType gameType,
            string? topicId,
            GameDifficulty difficulty);

        Task<(IReadOnlyList<GameMatchSession> Items, int TotalCount)> GetAllByUserAsync(
            string userId,
            GameType? gameType,
            string? topicId,
            GameDifficulty? difficulty,
            int pageNumber,
            int pageSize);

        Task<(IReadOnlyList<GameMatchSession> Items, int TotalCount)> GetPagedByGameTopicAsync(
            GameType? gameType,
            string? topicId,
            GameDifficulty? difficulty,
            int pageNumber,
            int pageSize);

        Task<(IReadOnlyList<(string UserId, GameType GameType, GameDifficulty GameDifficulty, string? TopicId, int BestScore)> Items, int TotalCount)> GetLeaderboardAsync(
            GameType? gameType,
            GameDifficulty? difficulty,
            string? topicId,
            int pageNumber,
            int pageSize);

        Task AddAsync(GameMatchSession session);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
