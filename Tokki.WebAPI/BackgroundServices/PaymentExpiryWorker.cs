using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.WebAPI.BackgroundServices
{
    public class PaymentExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentExpiryWorker> _logger;

        public PaymentExpiryWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentExpiryWorker đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireOldPayments();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi expire payment.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ExpireOldPayments()
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

            var now = DateTimeOffset.UtcNow;
            var expiredPayments = await repo.GetExpiredPendingPaymentsAsync(now);

            if (!expiredPayments.Any()) return;

            foreach (var payment in expiredPayments)
            {
                payment.Status = PaymentStatus.Expired;
                await repo.UpdateAsync(payment);
            }

            _logger.LogInformation("Đã expire {Count} payment(s).", expiredPayments.Count);
        }
    }
}