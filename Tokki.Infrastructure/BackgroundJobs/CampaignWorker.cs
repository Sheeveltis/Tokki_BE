using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Email Worker đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledJobs();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi Campaign Worker");
                }

                // --- THAY ĐỔI QUAN TRỌNG Ở ĐÂY ---
                // Thay vì ngủ 1 phút, ta chỉ ngủ 10 giây.
                // Điều này giúp email "Gửi ngay" sẽ được xử lý gần như tức thì.
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessScheduledJobs()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TokkiDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Đồng bộ giờ UTC+7
                var now = DateTime.UtcNow.AddHours(7);

                // LOGIC QUÉT JOB:
                // 1. Nếu Admin chọn "Gửi ngay" -> ScheduledTime = Now -> Thỏa mãn điều kiện (ScheduledTime <= now) -> Gửi luôn.
                // 2. Nếu Admin chọn "Lên lịch" (VD: 8h sáng mai) -> ScheduledTime > now -> Không tìm thấy -> Không làm gì cả (Chờ đến mai).

                var jobs = await context.EmailJobs
                    .Where(j => j.Status == EmailJobStatus.Pending && j.ScheduledTime <= now)
                    .ToListAsync();

                if (!jobs.Any()) return; // Không có việc thì nghỉ

                foreach (var job in jobs)
                {
                    _logger.LogInformation($"Bắt đầu gửi Job ID: {job.JobId}");

                    // Đánh dấu đang chạy
                    job.Status = EmailJobStatus.Processing;
                    await context.SaveChangesAsync();

                    try
                    {
                        // --- ĐOẠN NÀY DÙNG LOGIC JOIN MỚI (VIP/FREE) ---
                        var query = context.Accounts.AsNoTracking().Where(u => u.Status == AccountStatus.Active);

                        if (job.TargetGroup == UserTargetGroup.VipUsers)
                        {
                            // VIP: Có gói Active và chưa hết hạn
                            query = query.Where(u => context.Subscriptions.Any(s =>
                                s.UserId == u.UserId &&
                                s.Status == "Active" &&
                                s.EndDate > now
                            ));
                        }
                        else if (job.TargetGroup == UserTargetGroup.FreeUsers)
                        {
                            // FREE: Không có gói Active nào
                            query = query.Where(u => !context.Subscriptions.Any(s =>
                                s.UserId == u.UserId &&
                                s.Status == "Active" &&
                                s.EndDate > now
                            ));
                        }

                        var emails = await query.Select(u => u.Email).ToListAsync();
                        // =======================================================================
                        // 👉 THÊM ĐOẠN LOG NÀY ĐỂ DEBUG XEM AI LÀ VIP
                        // =======================================================================
                        _logger.LogInformation($"[DEBUG] Job ID {job.JobId} - Target: {job.TargetGroup}");
                        _logger.LogInformation($"[DEBUG] Tìm thấy tổng cộng: {emails.Count} user.");

                        if (emails.Any())
                        {
                            // Nối danh sách email thành 1 chuỗi để in ra dòng log
                            string listUser = string.Join(", ", emails);
                            _logger.LogInformation($"[DEBUG] Danh sách cụ thể: {listUser}");
                        }
                        else
                        {
                            _logger.LogWarning("[DEBUG] KHÔNG tìm thấy user nào thỏa mãn điều kiện!");
                        }
                        if (emails.Any())
                        {
                            // Gửi song song từng đợt 10 mail
                            int batchSize = 10;
                            for (int i = 0; i < emails.Count; i += batchSize)
                            {
                                var batch = emails.Skip(i).Take(batchSize);
                                var tasks = batch.Select(email => emailService.SendEmailAsync(email, job.Subject, job.Body));
                                await Task.WhenAll(tasks);
                            }
                        }

                        job.Status = EmailJobStatus.Sent;
                        job.SentAt = DateTime.UtcNow.AddHours(7);
                        _logger.LogInformation($"Job {job.JobId} hoàn tất. Gửi cho {emails.Count} người.");
                    }
                    catch (Exception ex)
                    {
                        job.Status = EmailJobStatus.Failed;
                        job.ErrorMessage = ex.Message;
                        _logger.LogError($"Job {job.JobId} thất bại: {ex.Message}");
                    }
                }

                // Lưu kết quả cuối cùng
                await context.SaveChangesAsync();
            }
        }
    }
}