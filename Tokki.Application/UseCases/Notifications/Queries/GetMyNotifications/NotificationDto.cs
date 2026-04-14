using System;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Notifications.Queries.GetMyNotifications
{
    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReferenceId { get; set; }
    }
}
