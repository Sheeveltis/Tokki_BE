using Microsoft.Extensions.Hosting;
using Tokki.Application.IRepositories;

namespace Tokki.Infrastructure.BackgroundJobs
{
    public class TokenCleanupJob : BackgroundService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepo;

        public TokenCleanupJob(IRefreshTokenRepository refreshTokenRepo)
        {
            _refreshTokenRepo = refreshTokenRepo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                // Tính thời gian đến 3:00 AM tiếp theo
                var next3AM = now.Date.AddDays(now.Hour >= 3 ? 1 : 0).AddHours(3);
                await Task.Delay(next3AM - now, stoppingToken);

                await _refreshTokenRepo.DeleteExpiredAsync(DateTime.UtcNow);
                await _refreshTokenRepo.DeleteRevokedAndExpiredAsync(DateTime.UtcNow);
            }
        }
    }
}