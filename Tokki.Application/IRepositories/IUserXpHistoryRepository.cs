using Tokki.Domain.Entities;

using Tokki.Domain.Enums;
namespace Tokki.Application.IRepositories
{
    public interface IUserXpHistoryRepository
    {
        Task AddAsync(UserXpHistory history);
        Task<long> GetTotalXpBySourceAndDateAsync(string userId, XpSource action, DateTime date);
        Task<int> CountActiveDaysAsync(string userId);
        Task<DateTime?> GetLastActivityDateAsync(string userId);
    }
}
