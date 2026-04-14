using System.Threading.Tasks;
using Tokki.Domain.Entities;
 
namespace Tokki.Application.IServices
 {
    public interface INotificationHubService
    {
        Task SendNotificationToUserAsync(string userId, object notification, int unreadCount);
    }
 }
