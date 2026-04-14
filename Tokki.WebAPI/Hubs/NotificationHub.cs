using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
 
namespace Tokki.WebAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            var userId = Context.UserIdentifier;

            _logger.LogInformation("=========================================");
            _logger.LogInformation("🔔 SignalR Connection Attempt");
            _logger.LogInformation("ConnectionId: {ConnId}", Context.ConnectionId);
            _logger.LogInformation("Detected UserIdentifier: {UserId}", userId ?? "NULL");

            if (user != null)
            {
                _logger.LogInformation("Claims list:");
                foreach (var claim in user.Claims)
                {
                    _logger.LogInformation(" - {Type}: {Value}", claim.Type, claim.Value);
                }
            }
            _logger.LogInformation("=========================================");

            await base.OnConnectedAsync();
        }
    }
}
