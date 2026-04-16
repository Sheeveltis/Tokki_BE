using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Gamification.DTOs;

namespace Tokki.Infrastructure.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserTitleService _userTitleService;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IUserXpHistoryRepository _userXpHistoryRepository;
        private readonly IIdGeneratorService _idGenerator;
        private const string LAZY_TITLE_NAME = "Con Lười";

        public GamificationService(IAccountRepository accountRepository, 
                                   IUserTitleService userTitleService,
                                   ISystemConfigRepository systemConfigRepository,
                                   IUserXpHistoryRepository userXpHistoryRepository,
                                   IIdGeneratorService idGenerator)
        {
            _accountRepository = accountRepository;
            _userTitleService = userTitleService;
            _systemConfigRepository = systemConfigRepository;
            _userXpHistoryRepository = userXpHistoryRepository;
            _idGenerator = idGenerator;
        }

        private async Task<int> GetTargetSecondsAsync()
        {
            string? value = await _systemConfigRepository.GetValueByKeyAsync("TARGET_STUDY_SECONDS");
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return 900; // Default fallback
        }

        public async Task CheckLoginGamificationAsync(Account user)
        {
            if (user == null) return;

            var vietnamNow = DateTime.UtcNow.AddHours(7);
            var today = vietnamNow.Date;
            var yesterday = today.AddDays(-1);

            HandleLazyReset(user, today, yesterday);

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
            int targetSeconds = await GetTargetSecondsAsync();

            // 1. Đồng bộ hóa logic reset (Stale Reset)
            HandleLazyReset(user, today, yesterday);

            // 2. Cộng dồn thời gian học
            user.DailyStudySeconds += seconds;
            bool isStreakCompletedNow = false;

            // 3. Kiểm tra mục tiêu streak
            bool completedToday = user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date == today;

            if (user.DailyStudySeconds >= targetSeconds && !completedToday)
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

                await _userXpHistoryRepository.AddAsync(new UserXpHistory
                {
                    Id = _idGenerator.Generate(21),
                    UserId = user.UserId,
                    Amount = bonusXP,
                    Action = XpSource.DailyStreak,
                    CreatedAt = vietnamNow
                });

                await _userTitleService.CheckAndUnlockTitlesAsync(user.UserId, TitleRequirementType.Streak, user.AchievedGoalStreak);
                var newlyUnlockedXp = await _userTitleService.CheckAndUnlockTitlesAsync(user.UserId, TitleRequirementType.XP, user.TotalXP);
                
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

        public async Task<StreakStatusDto> GetStreakStatusAsync(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return new StreakStatusDto();

            var vietnamNow = DateTime.UtcNow.AddHours(7);
            var today = vietnamNow.Date;
            var yesterday = today.AddDays(-1);
            int targetSeconds = await GetTargetSecondsAsync();

            var (currentStreak, dailySeconds, isCompletedToday) = CalculateCurrentStatus(user, today, yesterday);

            return new StreakStatusDto
            {
                CurrentStreak = currentStreak,
                MaxStreak = user.MaxStreak,
                IsCompletedToday = isCompletedToday,
                DailyStudySeconds = dailySeconds,
                TargetSeconds = targetSeconds
            };
        }

        private (int streak, double seconds, bool isCompletedToday) CalculateCurrentStatus(Account user, DateTime today, DateTime yesterday)
        {
            int currentStreak = user.AchievedGoalStreak;
            double dailySeconds = user.DailyStudySeconds;

            if (user.UpdatedAt.HasValue && user.UpdatedAt.Value.Date < today)
            {
                dailySeconds = 0;
            }

            if (user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date < yesterday)
            {
                currentStreak = 0;
            }

            bool isCompletedToday = user.LastStreakDate.HasValue && user.LastStreakDate.Value.Date == today;

            return (currentStreak, dailySeconds, isCompletedToday);
        }

        private void HandleLazyReset(Account user, DateTime today, DateTime yesterday)
        {
            var (streak, seconds, _) = CalculateCurrentStatus(user, today, yesterday);
            user.AchievedGoalStreak = streak;
            user.DailyStudySeconds = seconds;
        }
    }
}