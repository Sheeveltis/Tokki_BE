using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.BackgroundJobs
{
    public class BlogModerationBackgroundService : IBlogModerationBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BlogModerationBackgroundService> _logger;

        public BlogModerationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BlogModerationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ModerateBlogAsync(string blogId)
        {
            _logger.LogInformation("Starting AI moderation for blog: {BlogId}", blogId);
            
            using var scope = _scopeFactory.CreateScope();
            var blogRepo = scope.ServiceProvider.GetRequiredService<IBlogRepository>();
            var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var moderationService = scope.ServiceProvider.GetRequiredService<IContentModerationService>();
            var notificationHelper = scope.ServiceProvider.GetRequiredService<AppNotificationHelper>();
            var emailHelper = scope.ServiceProvider.GetRequiredService<EmailNotificationHelper>();

            var blog = await blogRepo.GetByIdAsync(blogId);
            if (blog == null)
            {
                _logger.LogWarning("Blog {BlogId} not found for moderation.", blogId);
                return;
            }

            // 1. Duyệt nội dung chính
            string fullText = $"{blog.Title} {blog.ShortDescription} {blog.Content}";
            var contentResult = await moderationService.CheckContentAsync(fullText);
            
            // Nếu AI bị lỗi (503, 429, ...) -> KHÔNG từ chối bài viết, mà chuyển cho Admin duyệt tay
            if (contentResult.IsError)
            {
                _logger.LogError("AI Moderation FAILED for Blog {BlogId}: {Error}. Falling back to manual review.", blogId, contentResult.ErrorMessage);
                blog.Status = BlogStatus.AIReviewFailed; // Chuyển trạng thái lỗi AI
                
                var sysConfigRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                var adminConfig = await sysConfigRepo.GetByKeyAsync("ADMIN_EMAIL");
                string adminEmail = adminConfig?.Value ?? "a.kiet098@gmail.com";
 
                await emailHelper.SendAIServiceFailureToAdminAsync(adminEmail, blog.Title, contentResult.ErrorMessage ?? "Unknown");
                await notificationHelper.SendBlogModerationResultAsync(blog.AuthorId, blog.Title, true, "Hệ thống AI đang tạm nghỉ, bài viết của bạn sẽ được kiểm tra thủ công.", blogId);
                
                await blogRepo.SaveChangesAsync(CancellationToken.None);
                return;
            }
 
            // 2. Duyệt từng Tag
            var dirtyTagNames = new List<string>();
            var tagsToRemove = new List<Tag>();
            
            if (blog.Tags != null && blog.Tags.Any())
            {
                foreach (var tag in blog.Tags)
                {
                    if (!tag.IsVerified)
                    {
                        var tagResult = await moderationService.CheckContentAsync(tag.Name);
                        if (tagResult.IsError) continue; // Nếu lỗi AI khi duyệt tag, tạm bỏ qua (giữ tag cũ)
 
                        if (!tagResult.IsClean)
                        {
                            dirtyTagNames.Add(tag.Name);
                            tagsToRemove.Add(tag);
                        }
                        else
                        {
                            tag.IsVerified = true; 
                        }
                    }
                }
            }
 
            try
            {
                if (!contentResult.IsClean || dirtyTagNames.Any())
                {
                    // Có vi phạm
                    string reason = "";
                    if (!contentResult.IsClean)
                    {
                        reason += $"Nội dung bài viết chứa từ khóa không phù hợp: {string.Join(", ", contentResult.BadWordsFound)}";
                    }
                    if (dirtyTagNames.Any())
                    {
                        if (!string.IsNullOrEmpty(reason)) reason += " | ";
                        reason += $"Các thẻ (tag) vi phạm đã bị loại bỏ: {string.Join(", ", dirtyTagNames)}";
                    }
 
                    _logger.LogWarning("Blog {BlogId} failed AI moderation.", blogId);
 
                    // Nếu nội dung chính bẩn -> Từ chối thẳng bở AI
                    if (!contentResult.IsClean)
                    {
                        blog.Status = BlogStatus.AIRejected;
                        
                        await notificationHelper.SendBlogModerationResultAsync(blog.AuthorId, blog.Title, false, reason, blogId);
                        
                        var sysConfigRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                        var adminConfig = await sysConfigRepo.GetByKeyAsync("ADMIN_EMAIL");
                        string adminEmail = adminConfig?.Value ?? "a.kiet098@gmail.com";
 
                        var user = await accountRepo.GetByIdAsync(blog.AuthorId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            await emailHelper.SendBlogAIRejectedAsync(user.Email, user.FullName ?? "Người dùng", blog.Title, reason, adminEmail);
                        }
                    }
                    else
                    {
                        // Chỉ có tag bẩn -> Chuyển Admin và xóa tag
                        blog.Status = BlogStatus.PendingApproval;
                        foreach (var t in tagsToRemove)
                        {
                            blog.Tags.Remove(t);
                            await blogRepo.DeleteTagAsync(t); 
                        }
                        await notificationHelper.SendBlogModerationResultAsync(blog.AuthorId, blog.Title, true, reason, blogId);
                    }
                }
                else
                {
                    // Hoàn toàn sạch
                    blog.Status = BlogStatus.PendingApproval;
                    await notificationHelper.SendBlogModerationResultAsync(blog.AuthorId, blog.Title, true, null, blogId);
                }

                await blogRepo.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during moderation for blog {BlogId}", blogId);
                // Nếu AI lỗi, set trạng thái lỗi AI để Admin biết
                blog.Status = BlogStatus.AIReviewFailed;
                await blogRepo.SaveChangesAsync(CancellationToken.None);
            }
        }
    }
}
