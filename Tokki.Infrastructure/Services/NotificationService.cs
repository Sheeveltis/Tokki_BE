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
        private readonly IIdGeneratorService _idGeneratorService;

        public NotificationService(INotificationRepository notificationRepository, IIdGeneratorService idGeneratorService)
        {
            _notificationRepository = notificationRepository;
            _idGeneratorService = idGeneratorService;
        }

        public async Task SendNotificationAsync(string userId, string title, string content, NotificationType type, string? referenceId = null)
        {
            var notification = new Notification
            {
                Id = _idGeneratorService.GenerateCustom(10), // Sử dụng IIdGeneratorService có sẵn
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ReferenceId = referenceId
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
            
            // Ở đây có thể tích hợp thêm SignalR để push thông báo realtime lên client
        }
    }
}
