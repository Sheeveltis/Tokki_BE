// Tokki.Infrastructure/BackgroundJobs/CampaignWorker.cs

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tokki.Infrastructure.Data;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.BackgroundJobs
{
    public class CampaignWorker : BackgroundService
    {
        private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(7);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampaignWorker> _logger;

        public CampaignWorker(IServiceProvider serviceProvider, ILogger<CampaignWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation("CampaignWorker constructed.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Email Worker đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledJobs(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi Campaign Worker");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessScheduledJobs(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TokkiDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // thời điểm chạy theo UTC+7
            var nowLocal = DateTime.UtcNow.AddHours(7);
            var nowLocalOffset = new DateTimeOffset(nowLocal, LocalOffset);

            var jobs = await context.EmailJobs
                .Where(j => j.Status == EmailJobStatus.Pending && j.ScheduledTime <= nowLocal)
                .ToListAsync(ct);

            if (!jobs.Any())
            {
                _logger.LogInformation("Không có job nào cần gửi.");
                return;
            }

            _logger.LogInformation($"Tìm thấy {jobs.Count} job cần xử lý.");

            foreach (var job in jobs)
            {
                if (ct.IsCancellationRequested) break;

                _logger.LogInformation($"Bắt đầu xử lý Job: {job.JobId}");

                // Đánh dấu Processing
                job.Status = EmailJobStatus.Processing;
                await context.SaveChangesAsync(ct);

                try
                {
                    var emails = new List<string>();

                    // Lấy email theo nhóm (KHÔNG dùng Subscriptions)
                    if (job.TargetGroup != UserTargetGroup.None)
                    {
                        IQueryable<Tokki.Domain.Entities.Account> query = context.Accounts
                            .AsNoTracking()
                            .Where(u => u.Status == AccountStatus.Active)
                            .Where(u => !string.IsNullOrEmpty(u.Email));

                        if (job.TargetGroup == UserTargetGroup.VipUsers)
                        {
                            query = query.Where(u =>
                                u.VipExpirationDate.HasValue &&
                                u.VipExpirationDate.Value > nowLocalOffset
                            );
                        }
                        else if (job.TargetGroup == UserTargetGroup.FreeUsers)
                        {
                            query = query.Where(u =>
                                !u.VipExpirationDate.HasValue ||
                                u.VipExpirationDate.Value <= nowLocalOffset
                            );
                        }
                        // UserTargetGroup.All: không lọc thêm

                        var groupEmails = await query.Select(u => u.Email).ToListAsync(ct);
                        emails.AddRange(groupEmails);

                        _logger.LogInformation($"[Job {job.JobId}] Lấy được {groupEmails.Count} email từ nhóm {job.TargetGroup}");
                    }

                    // Thêm email cá nhân
                    if (!string.IsNullOrWhiteSpace(job.SpecificEmails))
                    {
                        List<string>? specificEmails = null;

                        try
                        {
                            specificEmails = JsonSerializer.Deserialize<List<string>>(job.SpecificEmails);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"[Job {job.JobId}] SpecificEmails không phải JSON hợp lệ.");
                        }

                        if (specificEmails != null && specificEmails.Any())
                        {
                            emails.AddRange(specificEmails);
                            _logger.LogInformation($"[Job {job.JobId}] Thêm {specificEmails.Count} email cá nhân");
                        }
                    }

                    emails = emails
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Select(e => e.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    _logger.LogInformation($"[Job {job.JobId}] Tổng cộng {emails.Count} email.");

                    if (!emails.Any())
                    {
                        job.Status = EmailJobStatus.Failed;
                        job.ErrorMessage = "Không tìm thấy email nào phù hợp";
                        await context.SaveChangesAsync(ct);
                        continue;
                    }

                    // Gửi email theo batch
                    const int batchSize = 10;
                    for (int i = 0; i < emails.Count; i += batchSize)
                    {
                        var batch = emails.Skip(i).Take(batchSize).ToList();
                        var tasks = batch.Select(email => emailService.SendEmailAsync(email, job.Subject, job.Body));
                        await Task.WhenAll(tasks);
                    }

                    job.Status = EmailJobStatus.Sent;
                    job.SentAt = DateTime.UtcNow.AddHours(7);
                    job.ErrorMessage = null;

                    _logger.LogInformation($"Job {job.JobId} hoàn tất. Gửi cho {emails.Count} người.");
                }
                catch (Exception ex)
                {
                    job.Status = EmailJobStatus.Failed;
                    job.ErrorMessage = ex.Message;
                    _logger.LogError(ex, $"Job {job.JobId} thất bại");
                }

                await context.SaveChangesAsync(ct);
            }
        }
    }
}
