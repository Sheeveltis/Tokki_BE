using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserWeaknessRepository
    {
        Task<List<UserWeakness>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        Task AddAsync(UserWeakness entity, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}