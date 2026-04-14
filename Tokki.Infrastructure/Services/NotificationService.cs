using System;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
 
namespace Tokki.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly INotificationHubService _notificationHubService;
 
        public NotificationService(
            INotificationRepository notificationRepository, 
            IIdGeneratorService idGeneratorService,
            INotificationHubService notificationHubService,
            IAccountRepository accountRepository)
        {
            _notificationRepository = notificationRepository;
            _idGeneratorService = idGeneratorService;
            _notificationHubService = notificationHubService;
            _accountRepository = accountRepository;
        }
 
        public async Task SendNotificationAsync(string userId, string title, string content, NotificationType type, string? referenceId = null)
        {
            var notification = new Notification
            {
                Id = _idGeneratorService.GenerateCustom(10), 
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ReferenceId = referenceId
            };
 
            // Lưu vào DB (Repository đã tự động tăng UnreadNotificationCount trong Account)
            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
            
            // Lấy lại thông tin Account để lấy số lượng unread mới nhất
            var account = await _accountRepository.GetByIdAsync(userId);
            int unreadCount = account?.UnreadNotificationCount ?? 0;
 
            // Push qua SignalR realtime
            await _notificationHubService.SendNotificationToUserAsync(userId, notification, unreadCount);
        }
    }
}
