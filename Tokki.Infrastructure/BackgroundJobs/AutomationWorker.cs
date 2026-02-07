using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.BackgroundJobs
{
    public class AutomationWorker : BackgroundService
    {
        // UTC+7
        private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(7);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutomationWorker> _logger;

        public AutomationWorker(IServiceProvider serviceProvider, ILogger<AutomationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private static DateTimeOffset NowLocalOffset() =>
            DateTimeOffset.UtcNow.ToOffset(LocalOffset);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Automation Email Worker đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = NowLocalOffset();

                // nextRun = 02:00 mỗi ngày theo UTC+7
                var nextRun = new DateTimeOffset(now.Year, now.Month, now.Day, 2, 0, 0, LocalOffset);
                if (now >= nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);

                _logger.LogInformation($"Lần chạy kế tiếp lúc: {nextRun:yyyy-MM-dd HH:mm:ss} (UTC+7)");
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                    await RunDailyTasks(stoppingToken);
            }
        }

        public async Task RunDailyTasks(CancellationToken stoppingToken = default)
        {
            _logger.LogInformation("=== Bắt đầu chạy Daily Tasks ===");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TokkiDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var idGenerator = scope.ServiceProvider.GetRequiredService<IIdGeneratorService>();

            // thời gian hiện tại theo UTC+7
            var nowOffset = NowLocalOffset();
            var nowLocal = nowOffset.DateTime; // DateTime Kind=Unspecified (an toàn để dùng với DateTimeOffset constructor)

            try
            {
                var templates = await context.EmailTemplates
                    .AsNoTracking()
                    .Where(t => t.Status == EmailTemplateStatus.Active) // chỉ gửi template đang hoạt động
                    .Where(t => t.Type == EmailTemplateType.OfflineReminder
                             || t.Type == EmailTemplateType.VipExpiringReminder)
                    .Where(t => t.Value > 0)
                    .ToListAsync(stoppingToken);

                _logger.LogInformation($"Tìm thấy {templates.Count} email templates automation.");

                foreach (var template in templates)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        switch (template.Type)
                        {
                            case EmailTemplateType.OfflineReminder:
                                await ProcessOfflineReminderTemplate(
                                    context, emailService, idGenerator,
                                    template, nowLocal, nowOffset, stoppingToken);
                                break;

                            case EmailTemplateType.VipExpiringReminder:
                                await ProcessVipExpiringTemplate(
                                    context, emailService, idGenerator,
                                    template, nowLocal, nowOffset, stoppingToken);
                                break;

                            default:
                                _logger.LogWarning($"[Template:{template.TemplateId}] Type không hợp lệ: {template.Type}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi xử lý template: {template.TemplateId} - {template.TemplateName}");
                    }
                }

                _logger.LogInformation("=== Hoàn thành Daily Tasks ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy Daily Tasks");
            }
        }

        private static IQueryable<Account> ApplyTargetGroup(
            IQueryable<Account> query,
            UserTargetGroup targetGroup,
            DateTimeOffset nowLocalOffset)
        {
            return targetGroup switch
            {
                UserTargetGroup.None => query.Where(_ => false),
                UserTargetGroup.All => query,

                UserTargetGroup.VipUsers => query.Where(a =>
                    a.VipExpirationDate.HasValue &&
                    a.VipExpirationDate.Value > nowLocalOffset),

                UserTargetGroup.FreeUsers => query.Where(a =>
                    !a.VipExpirationDate.HasValue ||
                    a.VipExpirationDate.Value <= nowLocalOffset),

                _ => query
            };
        }

        private async Task ProcessOfflineReminderTemplate(
            TokkiDbContext context,
            IEmailService emailService,
            IIdGeneratorService idGenerator,
            EmailTemplate template,
            DateTime nowLocal,
            DateTimeOffset nowLocalOffset,
            CancellationToken ct)
        {
            var days = template.Value;
            var cutoff = nowLocal.AddDays(-days);

            var baseQuery = context.Accounts
                .AsNoTracking()
                .Where(a => a.Status == AccountStatus.Active)
                .Where(a => !string.IsNullOrEmpty(a.Email));

            baseQuery = ApplyTargetGroup(baseQuery, template.TargetGroup, nowLocalOffset);

            var users = await baseQuery
                .Where(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value <= cutoff)
                .Where(a => !context.EmailHistories.Any(h =>
                    h.UserId == a.UserId && h.TemplateId == template.TemplateId
                ))
                .ToListAsync(ct);

            if (!users.Any()) return;

            foreach (var user in users)
            {
                try
                {
                    var subject = template.Subject.Replace("{Value}", template.Value.ToString());
                    var body = template.Body
                        .Replace("{FullName}", user.FullName)
                        .Replace("{Value}", template.Value.ToString());

                    await emailService.SendEmailAsync(user.Email, subject, body);

                    context.EmailHistories.Add(new EmailHistory
                    {
                        Id = idGenerator.Generate(15),
                        UserId = user.UserId,
                        TemplateId = template.TemplateId,
                        SentAt = nowLocal
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[T1:{template.TemplateName}] Lỗi gửi email cho {user.Email}");
                }
            }

            await context.SaveChangesAsync(ct);
        }

        private async Task ProcessVipExpiringTemplate(
            TokkiDbContext context,
            IEmailService emailService,
            IIdGeneratorService idGenerator,
            EmailTemplate template,
            DateTime nowLocal,
            DateTimeOffset nowLocalOffset,
            CancellationToken ct)
        {
            var daysLeft = template.Value;

            // targetDate là DateTime Kind=Unspecified (an toàn)
            var targetDate = nowLocal.Date.AddDays(daysLeft);

            var start = new DateTimeOffset(targetDate, LocalOffset);
            var end = start.AddDays(1);

            var baseQuery = context.Accounts
                .AsNoTracking()
                .Where(a => a.Status == AccountStatus.Active)
                .Where(a => !string.IsNullOrEmpty(a.Email))
                .Where(a => a.VipExpirationDate.HasValue);

            baseQuery = ApplyTargetGroup(baseQuery, template.TargetGroup, nowLocalOffset);

            var users = await baseQuery
                .Where(a => a.VipExpirationDate!.Value >= start && a.VipExpirationDate.Value < end)
                .Where(a => !context.EmailHistories.Any(h =>
                    h.UserId == a.UserId && h.TemplateId == template.TemplateId
                ))
                .ToListAsync(ct);

            if (!users.Any()) return;

            foreach (var user in users)
            {
                try
                {
                    var endDateStr = user.VipExpirationDate!.Value
                        .ToOffset(LocalOffset)
                        .ToString("dd/MM/yyyy");

                    var subject = template.Subject.Replace("{Value}", template.Value.ToString());
                    var body = template.Body
                        .Replace("{FullName}", user.FullName)
                        .Replace("{EndDate}", endDateStr)
                        .Replace("{Value}", template.Value.ToString());

                    await emailService.SendEmailAsync(user.Email, subject, body);

                    context.EmailHistories.Add(new EmailHistory
                    {
                        Id = idGenerator.Generate(15),
                        UserId = user.UserId,
                        TemplateId = template.TemplateId,
                        SentAt = nowLocal
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[T2:{template.TemplateName}] Lỗi gửi email cho {user.Email}");
                }
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
