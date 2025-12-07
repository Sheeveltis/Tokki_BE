using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IAccountRepository
    {
        Task<bool> IsEmailExistsAsync(string email);
        Task AddAsync(Account user);
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<Account?> GetByEmailAsync(string email);
        Task AddSessionAsync(Session session);
        Task UpdateUserAsync(Account user);
        // Task UpdateEmailVerifiedAsync(string userId, bool isVerified);
        Task<Account?> GetByIdAsync(string userId);
    }
}