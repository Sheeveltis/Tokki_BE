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
            string gameId,
            string topicId,
            GameDifficulty difficulty);

        Task<(IReadOnlyList<GameMatchSession> Items, int TotalCount)> GetPagedByGameTopicAsync(
            string gameId,
            string topicId,
            int pageNumber,
            int pageSize);

        Task AddAsync(GameMatchSession session);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
