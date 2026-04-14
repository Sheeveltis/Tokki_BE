using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Notifications.DTOs;

namespace Tokki.Application.UseCases.Notifications.Queries.GetMyNotifications
{
    public class MyNotificationsResponse
    {
        public PagedResult<NotificationDto> Notifications { get; set; } = default!;
        public int TotalUnreadCount { get; set; }
    }
}
