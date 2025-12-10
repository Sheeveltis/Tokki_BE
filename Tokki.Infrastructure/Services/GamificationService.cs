using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
namespace Tokki.Infrastructure.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITitleRepository _titleRepository;
        private readonly TokkiDbContext _context;
        private const int TARGET_STUDY_SECONDS = 900; 
        private const string LAZY_TITLE_NAME = "Con Lười";

        public GamificationService(IAccountRepository accountRepository, ITitleRepository titleRepository, TokkiDbContext context)
        {
            _accountRepository = accountRepository;
            _titleRepository = titleRepository;
            _context = context;
        }

        private async Task UnlockAndEquipTitleAsync(Account user, Title title)
        {
            bool hasTitle = await _accountRepository.HasTitleAsync(user.UserId, title.TitleId);

            if (!hasTitle)
            {
                var newUnlock = new AccountTitle
                {
                    UserId = user.UserId,
                    TitleId = title.TitleId,
                    EarnedAt = DateTime.UtcNow.AddHours(7)
                };
                await _accountRepository.AddAccountTitleAsync(newUnlock);
            }

            if (user.CurrentTitleId != title.TitleId)
            {
                user.CurrentTitleId = title.TitleId;
            }
        }
        public async Task CheckLoginGamificationAsync(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return;

            if (user.LastLoginAt.HasValue)
            {
                var daysInactive = (DateTime.UtcNow.AddHours(7) - user.LastLoginAt.Value).TotalDays;

                if (daysInactive > 3)
                {
                    var lazyTitle = await _titleRepository.GetTitleByNameAsync("Con Lười");
                    if (lazyTitle != null)
                    {
                        await UnlockAndEquipTitleAsync(user, lazyTitle);
                    }
                }
            }
        }

        public async Task<bool> TrackStudyTimeAsync(string userId, double seconds)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.DailyStudySeconds += seconds;
            bool isStreakCompletedNow = false;

            bool completedToday = user.LastStreakDate.HasValue
                      && user.LastStreakDate.Value.Date == DateTime.UtcNow.AddHours(7).Date;

            if (user.DailyStudySeconds >= 900 && !completedToday)
            {
                isStreakCompletedNow = true;
                long bonusXP = 100; 

                user.TotalXP += bonusXP;

                var history = new UserXpHistory
                {
                    UserId = user.UserId,
                    Amount = bonusXP,
                    Action = "Daily Streak", 
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };

                _context.UserXpHistories.Add(history);

                if (user.CurrentTitleId != null)
                {
                }

                var bestTitle = await _titleRepository.GetTitleByXpAsync(user.TotalXP);

                if (bestTitle != null)
                {
                    await UnlockAndEquipTitleAsync(user, bestTitle);
                }
            }

            await _accountRepository.UpdateUserAsync(user);

            await _accountRepository.SaveChangesAsync(default);

            return isStreakCompletedNow;
        }
    }
}