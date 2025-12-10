using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tokki.Infrastructure.Data;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.BackgroundJobs
{
    public class AutomationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutomationWorker> _logger;

        public AutomationWorker(IServiceProvider serviceProvider, ILogger<AutomationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Automation Email Worker đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow.AddHours(7);

                // Chạy lúc 02:00 sáng mỗi ngày
                if (now.Hour == 2 && now.Minute == 0)
                {
                    await RunDailyTasks();
                    // Ngủ 1 tiếng để tránh chạy lại trong cùng 1 giờ
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }

                // Kiểm tra mỗi phút
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        public async Task RunDailyTasks()
        {
            _logger.LogInformation("=== Bắt đầu chạy Daily Tasks lúc 02:00 ===");

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TokkiDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var idGenerator = scope.ServiceProvider.GetRequiredService<IIdGeneratorService>(); // ✅ Thêm dòng này

                try
                {
                    // ========== OFFLINE REMINDERS ==========
                    await SendOfflineReminder(context, emailService, idGenerator, 30, "OFFLINE_30_DAYS");
                    await SendOfflineReminder(context, emailService, idGenerator, 90, "OFFLINE_90_DAYS");
                    await SendOfflineReminder(context, emailService, idGenerator, 180, "OFFLINE_180_DAYS");

                    // ========== VIP EXPIRING REMINDERS ==========
                    await SendVipExpiringReminder(context, emailService, idGenerator, 7, "VIP_EXPIRING_7_DAYS");
                    await SendVipExpiringReminder(context, emailService, idGenerator, 3, "VIP_EXPIRING_3_DAYS");
                    await SendVipExpiringReminder(context, emailService, idGenerator, 1, "VIP_EXPIRING_1_DAY");

                    _logger.LogInformation("=== Hoàn thành Daily Tasks ===");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi chạy Daily Tasks");
                }
            }
        }

        // ========== HÀM GỬI EMAIL OFFLINE (ĐÃ SỬA) ==========
        private async Task SendOfflineReminder(
            TokkiDbContext context,
            IEmailService emailService,
            IIdGeneratorService idGenerator, // ✅ Thêm parameter
            int days,
            string templateKey)
        {
            _logger.LogInformation($"[{templateKey}] Bắt đầu kiểm tra user offline >= {days} ngày...");

            // 1. Lấy template
            var template = await context.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateKey == templateKey);
            if (template == null)
            {
                _logger.LogWarning($"[{templateKey}] KHÔNG tìm thấy template trong DB!");
                return;
            }

            // 2. Tính ngày cutoff
            var cutoffDate = DateTime.UtcNow.AddHours(7).AddDays(-days);
            _logger.LogInformation($"[{templateKey}] Cutoff Date: {cutoffDate:yyyy-MM-dd HH:mm:ss}");

            // 3. Lấy danh sách user thỏa điều kiện
            var users = await context.Accounts
                .Where(u => u.LastLoginAt != null && u.LastLoginAt <= cutoffDate)
                .Where(u => !context.EmailHistories.Any(h =>
                    h.UserId == u.UserId &&
                    h.TemplateKey == templateKey
                ))
                .ToListAsync();

            _logger.LogInformation($"[{templateKey}] Tìm thấy {users.Count} user thỏa điều kiện.");

            if (!users.Any()) return;

            // 4. Gửi email + Lưu lịch sử
            int successCount = 0;
            foreach (var user in users)
            {
                try
                {
                    string body = template.Body.Replace("{FullName}", user.FullName);
                    await emailService.SendEmailAsync(user.Email, template.Subject, body);

                    // ✅ Lưu lịch sử đã gửi với NanoID
                    context.EmailHistories.Add(new EmailHistory
                    {
                        Id = idGenerator.Generate(), // ✅ Tạo ID bằng NanoID
                        UserId = user.UserId,
                        TemplateKey = templateKey,
                        SentAt = DateTime.UtcNow.AddHours(7)
                    });

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{templateKey}] Lỗi gửi email cho {user.Email}");
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"[{templateKey}] Đã gửi thành công {successCount}/{users.Count} email.");
        }

        // ========== HÀM NHẮC VIP SẮP HẾT HẠN (ĐÃ SỬA) ==========
        private async Task SendVipExpiringReminder(
            TokkiDbContext context,
            IEmailService emailService,
            IIdGeneratorService idGenerator, // ✅ Thêm parameter
            int daysLeft,
            string templateKey)
        {
            _logger.LogInformation($"[{templateKey}] Bắt đầu kiểm tra VIP hết hạn trong {daysLeft} ngày...");

            // 1. Lấy template
            var template = await context.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateKey == templateKey);
            if (template == null)
            {
                _logger.LogWarning($"[{templateKey}] KHÔNG tìm thấy template trong DB!");
                return;
            }

            // 2. Tính ngày hết hạn mục tiêu
            var targetDate = DateTime.UtcNow.AddHours(7).Date.AddDays(daysLeft);
            _logger.LogInformation($"[{templateKey}] Target Date: {targetDate:yyyy-MM-dd}");

            // 3. Lấy danh sách subscription sắp hết hạn
            var subs = await context.Subscriptions
                .Include(s => s.Account)
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate.Date == targetDate)
                .Where(s => !context.EmailHistories.Any(h =>
                    h.UserId == s.UserId &&
                    h.TemplateKey == templateKey
                ))
                .ToListAsync();

            _logger.LogInformation($"[{templateKey}] Tìm thấy {subs.Count} subscription thỏa điều kiện.");

            if (!subs.Any()) return;

            // 4. Gửi email + Lưu lịch sử
            int successCount = 0;
            foreach (var sub in subs)
            {
                if (sub.Account == null) continue;

                try
                {
                    string body = template.Body
                        .Replace("{FullName}", sub.Account.FullName)
                        .Replace("{EndDate}", sub.EndDate.ToString("dd/MM/yyyy"));

                    await emailService.SendEmailAsync(sub.Account.Email, template.Subject, body);

                    // ✅ Lưu lịch sử đã gửi với NanoID
                    context.EmailHistories.Add(new EmailHistory
                    {
                        Id = idGenerator.Generate(15), 
                        UserId = sub.UserId,
                        TemplateKey = templateKey,
                        SentAt = DateTime.UtcNow.AddHours(7)
                    });

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{templateKey}] Lỗi gửi email cho {sub.Account.Email}");
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"[{templateKey}] Đã gửi thành công {successCount}/{subs.Count} email.");
        }
    }
}