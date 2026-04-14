using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, OperationResult<bool>>
    {
        private readonly INotificationRepository _notificationRepository;

        public MarkAllAsReadCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<OperationResult<bool>> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
        {
            await _notificationRepository.MarkAllAsReadAsync(request.UserId);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, "Đã đánh dấu tất cả là đã đọc.");
        }
    }
}
