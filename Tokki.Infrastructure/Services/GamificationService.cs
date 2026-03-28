using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Gamification.Commands.AddGameXp;
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

        public async Task CheckLoginGamificationAsync(Account user)
        {
            if (user == null) return;

            var vietnamNow = DateTime.UtcNow.AddHours(7);
            HandleLazyReset(user, vietnamNow.Date, vietnamNow.Date.AddDays(-1));

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

            var vietnamNow = DateTime.UtcNow.AddHours(7);
            var today = vietnamNow.Date;
            var yesterday = today.AddDays(-1);

            if (user.UpdatedAt.HasValue && user.UpdatedAt.Value.Date < today)
            {
                user.DailyStudySeconds = 0;

                if (user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date < yesterday)
                {
                    user.AchievedGoalStreak = 0;
                }
            }

            user.DailyStudySeconds += seconds;
            bool isStreakCompletedNow = false;

            bool completedToday = user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date == today;

            if (user.DailyStudySeconds >= TARGET_STUDY_SECONDS && !completedToday)
            {
                isStreakCompletedNow = true;

                if (user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date == yesterday)
                {
                    user.AchievedGoalStreak++;
                }
                else
                {
                    user.AchievedGoalStreak = 1;
                }

                if (user.AchievedGoalStreak > user.MaxStreak)
                {
                    user.MaxStreak = user.AchievedGoalStreak;
                }

                user.LastStreakDate = today;

                long bonusXP = 100;
                user.TotalXP += bonusXP;

                _context.UserXpHistories.Add(new UserXpHistory
                {
                    Id = _idGenerator.Generate(),
                    UserId = user.UserId,
                    Amount = bonusXP,
                    Action = "Daily Streak Achievement",
                    CreatedAt = vietnamNow
                });

                var bestTitle = await _titleRepository.GetTitleByXpAsync(user.TotalXP);
                if (bestTitle != null && user.CurrentTitleId != bestTitle.TitleId)
                {
                    user.CurrentTitleId = bestTitle.TitleId;
                }
            }

            user.UpdatedAt = vietnamNow;

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(default);

            return isStreakCompletedNow;
        }
        private void HandleLazyReset(Account user, DateTime today, DateTime yesterday)
        {
            if (user.UpdatedAt.HasValue && user.UpdatedAt.Value.Date < today)
            {
                user.DailyStudySeconds = 0;

                if (user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date < yesterday)
                {
                    user.AchievedGoalStreak = 0;
                }
            }
        }

        public async Task<AddGameXpResultDto> AddGameXpAsync(string userId, long amount)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException($"Không tìm thấy user với id: {userId}");

            var vietnamNow = DateTime.UtcNow.AddHours(7);

            user.TotalXP += amount;

            bool isNewTitleUnlocked = false;
            string? newTitleName = null;
            string? newTitleColorHex = null;

            var bestTitle = await _titleRepository.GetTitleByXpAsync(user.TotalXP);
            if (bestTitle != null && user.CurrentTitleId != bestTitle.TitleId)
            {
                user.CurrentTitleId = bestTitle.TitleId;
                isNewTitleUnlocked = true;
                newTitleName = bestTitle.Name;
                newTitleColorHex = bestTitle.ColorHex;
            }

            user.UpdatedAt = vietnamNow;
            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(default);

            return new AddGameXpResultDto
            {
                TotalXP = user.TotalXP,
                XpAdded = amount,
                IsNewTitleUnlocked = isNewTitleUnlocked,
                NewTitleName = newTitleName,
                NewTitleColorHex = newTitleColorHex
            };
        }
    }
}