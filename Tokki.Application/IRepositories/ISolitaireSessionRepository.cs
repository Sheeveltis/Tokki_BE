using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface ISolitaireSessionRepository
    {
        Task<GameMatchSession?> GetByUserGameAsync(
            string userId,
            string gameId,
            GameDifficulty difficulty);

        Task<(IReadOnlyList<GameMatchSession> Items, int TotalCount)> GetPagedByGameAsync(
            string gameId,
            GameDifficulty difficulty,
            int pageNumber,
            int pageSize);

        Task AddAsync(GameMatchSession session);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
