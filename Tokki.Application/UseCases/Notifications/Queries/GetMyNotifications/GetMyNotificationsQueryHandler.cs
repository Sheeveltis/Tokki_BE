using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Notifications.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Notifications.Queries.GetMyNotifications
{
    public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, OperationResult<MyNotificationsResponse>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IAccountRepository _accountRepository;

        public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository, IAccountRepository accountRepository)
        {
            _notificationRepository = notificationRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<MyNotificationsResponse>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return OperationResult<MyNotificationsResponse>.Failure("Không tìm thấy người dùng.", 404);
            }

            bool? isReadFilter = request.Filter switch
            {
                NotificationReadFilter.Read => true,
                NotificationReadFilter.Unread => false,
                _ => null
            };

            var totalCount = await _notificationRepository.CountTotalByUserIdAsync(request.UserId, isReadFilter);
            var notifications = await _notificationRepository.GetPagedByUserIdAsync(request.UserId, request.PageNumber, request.PageSize, isReadFilter);
            var totalUnreadCount = await _notificationRepository.CountUnreadAsync(request.UserId);
            
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

            var result = new MyNotificationsResponse
            {
                Notifications = PagedResult<NotificationDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize),
                TotalUnreadCount = totalUnreadCount
            };

            return OperationResult<MyNotificationsResponse>.Success(result, 200);
        }
    }
}
