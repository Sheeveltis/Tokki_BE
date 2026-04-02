using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    // Application/IRepositories/IRefreshTokenRepository.cs
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string hash);
        Task<List<RefreshToken>> GetAllByUserIdAsync(string userId);
        Task AddAsync(RefreshToken token);
        Task DeleteExpiredAsync(DateTime now);
        Task DeleteRevokedAndExpiredAsync(DateTime now);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
