using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IServices
{
    public interface IChatNotificationService
    {
        Task SendMessageToRoomAsync(string roomId, ChatMessage message);
        Task NotifyRoomClosedAsync(string roomId);
    }
}