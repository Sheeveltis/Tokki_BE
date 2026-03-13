using Microsoft.EntityFrameworkCore;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Worker
{
    public class WordleGeneratorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WordleGeneratorWorker> _logger;

        public WordleGeneratorWorker(IServiceProvider serviceProvider, ILogger<WordleGeneratorWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateDailyWordlesAsync(stoppingToken);

                    var now = DateTime.Now; 
                    var tomorrowMidnight = now.Date.AddDays(1); 
                    var delay = tomorrowMidnight - now;

                    _logger.LogInformation($"[WordleWorker] Đã xong việc. Giờ máy chủ: {now:HH:mm:ss}. Ngủ {delay.TotalHours:F1} tiếng tới {tomorrowMidnight}...");

                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WordleWorker] Lỗi rồi!");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task GenerateDailyWordlesAsync(CancellationToken token)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TokkiDbContext>();

                var idGenerator = scope.ServiceProvider.GetRequiredService<IIdGeneratorService>();

                var today = DateOnly.FromDateTime(DateTime.Now);

                var configs = new List<(WordleLevel Level, int Length)>
                {
                    (WordleLevel.Easy, 2),
                    (WordleLevel.Medium, 3),
                    (WordleLevel.Hard, 4)
                };

                foreach (var config in configs)
                {
                    bool exists = await context.DailyWordles
                        .AnyAsync(x => x.GameDate == today && x.Level == config.Level, token);

                    if (exists) continue;

                    _logger.LogInformation($"[WordleWorker] Tạo đề ngày {today} - Level {config.Level}...");

                    var vocab = await context.Vocabularies
                        .Where(v => v.Text.Length == config.Length && !v.Text.Contains(" "))
                        .OrderBy(x => Guid.NewGuid())
                        .FirstOrDefaultAsync(token);

                    if (vocab != null)
                    {
                        var newGame = new DailyWordle
                        {
                            DailyWordleId = idGenerator.Generate(10),

                            GameDate = today,
                            Level = config.Level,
                            Word = vocab.Text,
                            VocabularyId = vocab.VocabularyId
                        };

                        context.DailyWordles.Add(newGame);
                    }
                    else
                    {
                        _logger.LogWarning($"[WordleWorker] Không tìm thấy từ vựng nào có độ dài {config.Length}!");
                    }
                }

                await context.SaveChangesAsync(token);
            }
        }
    }
}
