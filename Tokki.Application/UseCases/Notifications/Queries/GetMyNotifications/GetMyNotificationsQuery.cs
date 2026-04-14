using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Notifications.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Notifications.Queries.GetMyNotifications
{
    public class GetMyNotificationsQuery : IRequest<OperationResult<MyNotificationsResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string UserId { get; set; } = string.Empty;
        public NotificationReadFilter Filter { get; set; } = NotificationReadFilter.All;
    }
}
