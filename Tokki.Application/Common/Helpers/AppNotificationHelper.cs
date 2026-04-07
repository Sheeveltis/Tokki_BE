using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.Common.Helpers
{
    public class AppNotificationHelper
    {
        private readonly INotificationService _notificationService;
        private readonly ISystemConfigRepository _systemConfigRepository;

        public AppNotificationHelper(INotificationService notificationService, ISystemConfigRepository systemConfigRepository)
        {
            _notificationService = notificationService;
            _systemConfigRepository = systemConfigRepository;
        }

        private async Task<string> GetConfigValueAsync(string key, string defaultValue)
        {
            var config = await _systemConfigRepository.GetByKeyAsync(key);
            return config?.Value ?? defaultValue;
        }

        public async Task SendBlogModerationResultAsync(string userId, string blogTitle, bool isApproved, string? reason = null, string? blogId = null)
        {
            string titleKey = isApproved ? "NOTI_APPROVE_BLOG_TITLE" : "NOTI_REJECT_BLOG_TITLE";
            string contentKey = isApproved ? "NOTI_APPROVE_BLOG_CONTENT" : "NOTI_REJECT_BLOG_CONTENT";

            string titleTemplate = await GetConfigValueAsync(titleKey, isApproved ? "Bài viết đã được duyệt" : "Bài viết bị từ chối");
            string contentTemplate = await GetConfigValueAsync(contentKey, isApproved 
                ? "Chúc mừng! Bài viết '{BlogTitle}' của bạn đã vượt qua vòng kiểm duyệt tự động."
                : "Rất tiếc! Bài viết '{BlogTitle}' của bạn không vượt qua bộ lọc nội dung. Lý do: {Reason}.");

            string finalContent = contentTemplate
                .Replace("{BlogTitle}", blogTitle)
                .Replace("{Reason}", reason ?? "Vi phạm tiêu chuẩn cộng đồng");

            await _notificationService.SendNotificationAsync(
                userId,
                titleTemplate,
                finalContent,
                isApproved ? NotificationType.BlogApproval : NotificationType.ModerationWarning,
                blogId
            );
        }

        public async Task SendBlogSubmissionReceivedAsync(string userId, string blogTitle, string? blogId = null)
        {
            string titleTemplate = await GetConfigValueAsync("NOTI_SUBMIT_BLOG_TITLE", "Đã nhận bài viết");
            string contentTemplate = await GetConfigValueAsync("NOTI_SUBMIT_BLOG_CONTENT", "Bài viết '{BlogTitle}' của bạn đã được gửi thành công và đang trong quá trình kiểm duyệt tự động.");

            string finalContent = contentTemplate.Replace("{BlogTitle}", blogTitle);

            await _notificationService.SendNotificationAsync(
                userId,
                titleTemplate,
                finalContent,
                NotificationType.BlogApproval,
                blogId
            );
        }

        // Linh hoạt cho các trường hợp khác sau này
        public async Task SendGenericNotificationAsync(string userId, string configKeyPrefix, Dictionary<string, string> placeholders, NotificationType type, string? referenceId = null)
        {
            string titleTemplate = await GetConfigValueAsync($"{configKeyPrefix}_TITLE", "Thông báo mới");
            string contentTemplate = await GetConfigValueAsync($"{configKeyPrefix}_CONTENT", "Bạn có một thông báo mới từ hệ thống.");

            string finalContent = contentTemplate;
            foreach (var placeholder in placeholders)
            {
                finalContent = finalContent.Replace(placeholder.Key, placeholder.Value);
            }

            await _notificationService.SendNotificationAsync(userId, titleTemplate, finalContent, type, referenceId);
        }
    }
}
