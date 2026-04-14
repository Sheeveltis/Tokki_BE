using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
 
namespace Tokki.WebAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Hub này chủ yếu dùng để Client lắng nghe, 
        // Server sẽ push thông báo thông qua IHubContext
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
