using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllAsReadCommand : IRequest<OperationResult<bool>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
