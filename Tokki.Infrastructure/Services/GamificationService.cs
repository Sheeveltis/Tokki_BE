using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Tokki.Application.Common.Helpers; 
namespace Tokki.Infrastructure.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITitleRepository _titleRepository;
        private readonly TokkiDbContext _context;
        private readonly IIdGeneratorService _idGenerator;
        private const int TARGET_STUDY_SECONDS = 900;
        private const string LAZY_TITLE_NAME = "Con Lười";

        public GamificationService(IAccountRepository accountRepository, ITitleRepository titleRepository, TokkiDbContext context, IIdGeneratorService idGenerator)
        {
            _accountRepository = accountRepository;
            _titleRepository = titleRepository;
            _context = context;
            _idGenerator = idGenerator;
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

            user.CurrentTitleId = title.TitleId;
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
            DateTime vietnamNow = DateTime.UtcNow.AddHours(7);

            bool completedToday = user.LastStreakDate.HasValue
                      && user.LastStreakDate.Value.Date == vietnamNow.Date;

            if (user.DailyStudySeconds >= TARGET_STUDY_SECONDS && !completedToday)
            {
                isStreakCompletedNow = true;

                var yesterday = vietnamNow.Date.AddDays(-1);
                if (user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date == yesterday)
                {
                    user.CurrentStreak++;
                }
                else
                {
                    user.CurrentStreak = 1;
                }

                if (user.CurrentStreak > user.MaxStreak)
                {
                    user.MaxStreak = user.CurrentStreak;
                }

                user.LastStreakDate = vietnamNow.Date;

                int oldLevel = LevelEngine.GetLevel(user.TotalXP);

                long bonusXP = 100;
                user.TotalXP += bonusXP;

                int newLevel = LevelEngine.GetLevel(user.TotalXP);

                var history = new UserXpHistory
                {
                    Id = _idGenerator.Generate(),
                    UserId = user.UserId,
                    Amount = bonusXP,
                    Action = "Daily Streak",
                    CreatedAt = vietnamNow
                };
                _context.UserXpHistories.Add(history);

                var bestTitle = await _titleRepository.GetTitleByXpAsync(user.TotalXP);

                if (bestTitle != null && user.CurrentTitleId != bestTitle.TitleId)
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