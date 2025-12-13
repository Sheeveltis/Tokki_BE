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
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampaignWorker> _logger;

        public CampaignWorker(IServiceProvider serviceProvider, ILogger<CampaignWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            Console.WriteLine("🔧 CampaignWorker CONSTRUCTOR được gọi!");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("🚀 CampaignWorker ExecuteAsync BẮT ĐẦU!");
            _logger.LogInformation("Campaign Email Worker đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"🔄 [{DateTime.Now:HH:mm:ss}] Campaign Worker check jobs...");
                    await ProcessScheduledJobs();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi Campaign Worker: {ex.Message}");
                    _logger.LogError(ex, "Lỗi Campaign Worker");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessScheduledJobs()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TokkiDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var now = DateTime.UtcNow.AddHours(7);

                // ✅ SO SÁNH TRỰC TIẾP VỚI ENUM (EF tự convert sang 0)
                var jobs = await context.EmailJobs
                    .Where(j => j.Status == EmailJobStatus.Pending && j.ScheduledTime <= now)
                    .ToListAsync();

                if (!jobs.Any())
                {
                    _logger.LogInformation("Không có job nào cần gửi");
                    return;
                }

                _logger.LogInformation($"Tìm thấy {jobs.Count} job cần xử lý");

                foreach (var job in jobs)
                {
                    _logger.LogInformation($"Bắt đầu xử lý Job: {job.JobId}");

                    // Đánh dấu đang xử lý
                    job.Status = EmailJobStatus.Processing; // EF convert thành 1
                    await context.SaveChangesAsync();

                    try
                    {
                        var emails = new List<string>();

                        // Lấy email theo nhóm
                        if (job.TargetGroup != UserTargetGroup.None)
                        {
                            var query = context.Accounts.AsNoTracking()
                                .Where(u => u.Status == AccountStatus.Active);

                            if (job.TargetGroup == UserTargetGroup.VipUsers)
                            {
                                query = query.Where(u => context.Subscriptions.Any(s =>
                                    s.UserId == u.UserId &&
                                    s.Status == SubscriptionStatus.Active &&
                                    s.EndDate > now
                                ));
                            }
                            else if (job.TargetGroup == UserTargetGroup.FreeUsers)
                            {
                                query = query.Where(u => !context.Subscriptions.Any(s =>
                                    s.UserId == u.UserId &&
                                    s.Status == SubscriptionStatus.Active &&
                                    s.EndDate > now
                                ));
                            }

                            var groupEmails = await query.Select(u => u.Email).ToListAsync();
                            emails.AddRange(groupEmails);
                            _logger.LogInformation($"[Job {job.JobId}] Lấy được {groupEmails.Count} email từ nhóm {job.TargetGroup}");
                        }

                        // Thêm email cá nhân
                        if (!string.IsNullOrEmpty(job.SpecificEmails))
                        {
                            var specificEmails = JsonSerializer.Deserialize<List<string>>(job.SpecificEmails);
                            if (specificEmails != null && specificEmails.Any())
                            {
                                emails.AddRange(specificEmails);
                                _logger.LogInformation($"[Job {job.JobId}] Thêm {specificEmails.Count} email cá nhân");
                            }
                        }

                        // Loại bỏ trùng lặp
                        emails = emails.Distinct().ToList();
                        _logger.LogInformation($"[Job {job.JobId}] Tổng cộng {emails.Count} email");

                        if (!emails.Any())
                        {
                            _logger.LogWarning($"[Job {job.JobId}] KHÔNG tìm thấy email nào!");
                            job.Status = EmailJobStatus.Failed; // EF convert thành 3
                            job.ErrorMessage = "Không tìm thấy email nào phù hợp";
                            await context.SaveChangesAsync();
                            continue;
                        }

                        // Gửi email theo batch
                        int batchSize = 10;
                        for (int i = 0; i < emails.Count; i += batchSize)
                        {
                            var batch = emails.Skip(i).Take(batchSize);
                            var tasks = batch.Select(email => emailService.SendEmailAsync(email, job.Subject, job.Body));
                            await Task.WhenAll(tasks);
                        }

                        job.Status = EmailJobStatus.Sent; // EF convert thành 2
                        job.SentAt = DateTime.UtcNow.AddHours(7);
                        _logger.LogInformation($"✅ Job {job.JobId} hoàn tất. Gửi cho {emails.Count} người.");
                    }
                    catch (Exception ex)
                    {
                        job.Status = EmailJobStatus.Failed; // EF convert thành 3
                        job.ErrorMessage = ex.Message;
                        _logger.LogError(ex, $"Job {job.JobId} thất bại");
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}