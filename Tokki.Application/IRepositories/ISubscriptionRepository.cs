using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface ISubscriptionRepository
    {
        Task AddAsync(Subscription subscription);

        Task<List<Subscription>> GetByUserIdAsync(string userId);
    }
}