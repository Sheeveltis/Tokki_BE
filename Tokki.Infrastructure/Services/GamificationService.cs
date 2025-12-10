using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITitleRepository _titleRepository;
        private const int TARGET_STUDY_SECONDS = 900; 
        private const string LAZY_TITLE_NAME = "Con Lười";

        public GamificationService(IAccountRepository accountRepository, ITitleRepository titleRepository)
        {
            _accountRepository = accountRepository;
            _titleRepository = titleRepository;
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
                    var lazyTitle = await _titleRepository.GetTitleByNameAsync(LAZY_TITLE_NAME);
                    if (lazyTitle != null && user.CurrentTitleId != lazyTitle.TitleId)
                    {
                        user.CurrentTitleId = lazyTitle.TitleId;
                        await _accountRepository.UpdateUserAsync(user);
                        await _accountRepository.SaveChangesAsync(default);
                    }
                }
            }
        }

        public async Task<bool> TrackStudyTimeAsync(string userId, double seconds)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return false;

            var today = DateTime.UtcNow.AddHours(7).Date;

            if (user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date != today)
            {
               
            }

            user.DailyStudySeconds += seconds;

            bool isStreakCompletedNow = false;

            bool alreadyCompletedToday = user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date == today;

            if (user.DailyStudySeconds >= TARGET_STUDY_SECONDS && !alreadyCompletedToday)
            {
                isStreakCompletedNow = true;

                if (user.LastStreakDate.HasValue)
                {
                    var yesterday = today.AddDays(-1);
                    if (user.LastStreakDate.Value.Date == yesterday)
                    {
                        user.CurrentStreak++;
                    }
                    else
                    {
                        user.CurrentStreak = 1;
                    }
                }
                else
                {
                    user.CurrentStreak = 1; 
                }

                if (user.CurrentStreak > user.MaxStreak) user.MaxStreak = user.CurrentStreak;

                user.LastStreakDate = DateTime.UtcNow.AddHours(7);

                user.TotalXP += 100; 

                if (user.CurrentTitle != null && user.CurrentTitle.Name == LAZY_TITLE_NAME)
                {
                    var realTitle = await _titleRepository.GetTitleByXpAsync(user.TotalXP);
                    user.CurrentTitleId = realTitle?.TitleId; 
                }
            }

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(default);

            return isStreakCompletedNow;
        }
    }
}