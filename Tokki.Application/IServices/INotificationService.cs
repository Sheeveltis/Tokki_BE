using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.IServices
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string content, NotificationType type, string? referenceId = null);
    }
}
