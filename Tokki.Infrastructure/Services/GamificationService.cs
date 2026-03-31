using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Gamification.Commands.AddGameXp;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserTitleService _userTitleService;
        private readonly TokkiDbContext _context;
        private readonly IIdGeneratorService _idGenerator;
        private const int TARGET_STUDY_SECONDS = 900;
        private const string LAZY_TITLE_NAME = "Con Lười";

        public GamificationService(IAccountRepository accountRepository, 
                                   IUserTitleService userTitleService,
                                   TokkiDbContext context, 
                                   IIdGeneratorService idGenerator)
        {
            _accountRepository = accountRepository;
            _userTitleService = userTitleService;
            _context = context;
            _idGenerator = idGenerator;
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
                    await _userTitleService.CheckAndUnlockTitlesAsync(user.UserId, TitleRequirementType.InactivityDays, (long)daysInactive);
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

                // Check Streak-based Titles
                await _userTitleService.CheckAndUnlockTitlesAsync(user.UserId, TitleRequirementType.Streak, user.AchievedGoalStreak);
                
                // Check XP-based Titles
                var newlyUnlockedXp = await _userTitleService.CheckAndUnlockTitlesAsync(user.UserId, TitleRequirementType.XP, user.TotalXP);
                
                // If anything new unlocked and it's better than current, maybe auto-equip?
                // For now, let's keep it manual except if they don't have current title
                if (newlyUnlockedXp.Any() && string.IsNullOrEmpty(user.CurrentTitleId))
                {
                    user.CurrentTitleId = newlyUnlockedXp.Last().TitleId;
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

            // Check XP Titles
            var newlyUnlocked = await _userTitleService.CheckAndUnlockTitlesAsync(user.UserId, TitleRequirementType.XP, user.TotalXP);
            
            bool isNewTitleUnlocked = newlyUnlocked.Any();
            string? newTitleName = newlyUnlocked.LastOrDefault()?.Name;
            string? newTitleColorHex = newlyUnlocked.LastOrDefault()?.ColorHex;

            if (isNewTitleUnlocked && string.IsNullOrEmpty(user.CurrentTitleId))
            {
                user.CurrentTitleId = newlyUnlocked.Last().TitleId;
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