using Microsoft.AspNetCore.SignalR;
using Tokki.Application.IServices;
using Tokki.WebAPI.Hubs;
using System.Threading.Tasks;
 
namespace Tokki.WebAPI.Services
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
 
        public NotificationHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }
 
        public async Task SendNotificationToUserAsync(string userId, object notification, int unreadCount)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification, unreadCount);
        }
    }
}
