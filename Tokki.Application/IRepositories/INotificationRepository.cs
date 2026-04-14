using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task<Notification?> GetByIdAsync(string id);
        Task<List<Notification>> GetByUserIdAsync(string userId, int count = 20);
        Task<int> CountUnreadAsync(string userId);
        Task MarkAsReadAsync(string id);
        Task MarkAllAsReadAsync(string userId);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
