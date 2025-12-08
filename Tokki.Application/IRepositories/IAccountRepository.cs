using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

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
        Task<Account?> GetByIdAsync(string userId);
        Task<List<string>> GetEmailsByTargetGroupAsync(UserTargetGroup targetGroup);
    }
}