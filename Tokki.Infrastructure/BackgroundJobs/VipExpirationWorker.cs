using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.BackgroundJobs
{
    public class VipExpirationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VipExpirationWorker> _logger;

        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        public VipExpirationWorker(IServiceProvider serviceProvider, ILogger<VipExpirationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VipExpirationWorker (High Precision) đã khởi động");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredVips(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét VIP hết hạn.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessExpiredVips(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TokkiDbContext>();

            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));

            var expiredVips = await context.Accounts
                .Where(u => u.Role == AccountRole.Vip
                            && u.VipExpirationDate.HasValue
                            && u.VipExpirationDate.Value <= now)
                .ToListAsync(ct);

            if (expiredVips.Any())
            {
                _logger.LogInformation($"Tìm thấy {expiredVips.Count} tài khoản VIP đã hết hạn lúc {now:HH:mm:ss}.");

                foreach (var user in expiredVips)
                {
                    user.Role = AccountRole.User;

                    _logger.LogInformation($"[AUTO-DOWNGRADE] User {user.UserId} ({user.Email}) đã bị hạ cấp.");
                }

                await context.SaveChangesAsync(ct);

                _logger.LogInformation("Đã cập nhật Database thành công.");
            }
        }
    }
}