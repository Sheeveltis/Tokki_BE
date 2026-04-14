using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Notifications.Queries.GetMyNotifications
{
    public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, OperationResult<PagedResult<NotificationDto>>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IAccountRepository _accountRepository;

        public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository, IAccountRepository accountRepository)
        {
            _notificationRepository = notificationRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<PagedResult<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return OperationResult<PagedResult<NotificationDto>>.Failure("Không tìm thấy người dùng.", 404);
            }

            var totalCount = await _notificationRepository.CountTotalByUserIdAsync(request.UserId);
            var notifications = await _notificationRepository.GetPagedByUserIdAsync(request.UserId, request.PageNumber, request.PageSize);
            
            var dtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReferenceId = n.ReferenceId
            }).ToList();

            return OperationResult<PagedResult<NotificationDto>>.Success(
                PagedResult<NotificationDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize),
                200
            );
        }
    }
}
