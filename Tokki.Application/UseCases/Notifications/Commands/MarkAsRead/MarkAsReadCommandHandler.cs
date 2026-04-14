using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Notifications.Commands.MarkAsRead
{
    public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, OperationResult<bool>>
    {
        private readonly INotificationRepository _notificationRepository;

        public MarkAsReadCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<OperationResult<bool>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId);
            
            if (notification == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy thông báo.", 404);
            }

            if (notification.UserId != request.UserId)
            {
                return OperationResult<bool>.Failure("Bạn không có quyền cập nhật thông báo này.", 403);
            }

            if (!notification.IsRead)
            {
                await _notificationRepository.MarkAsReadAsync(request.NotificationId);
                await _notificationRepository.SaveChangesAsync(cancellationToken);
            }

            return OperationResult<bool>.Success(true, 200, "Đã đánh dấu đã đọc.");
        }
    }
}
