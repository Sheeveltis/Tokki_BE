using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Notifications.Commands.MarkAsRead
{
    public class MarkAsReadCommand : IRequest<OperationResult<bool>>
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty; // Sẽ dùng để verify quyền sở hữu
    }
}
