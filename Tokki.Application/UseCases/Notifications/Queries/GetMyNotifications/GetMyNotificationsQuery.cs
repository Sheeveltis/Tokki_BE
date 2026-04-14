using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Notifications.Queries.GetMyNotifications
{
    public class GetMyNotificationsQuery : IRequest<OperationResult<PagedResult<NotificationDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string UserId { get; set; } = string.Empty; // Sẽ được lấy từ Token trong Controller
    }
}
