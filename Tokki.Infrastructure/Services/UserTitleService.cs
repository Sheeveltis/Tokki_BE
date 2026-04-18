using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.Common.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Tokki.Infrastructure.Services
{
    public class UserTitleService : IUserTitleService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITitleRepository _titleRepository;
        private readonly IUserXpHistoryRepository _userXpHistoryRepository;

        public UserTitleService(IAccountRepository accountRepository, 
                               ITitleRepository titleRepository,
                               IUserXpHistoryRepository userXpHistoryRepository)
        {
            _accountRepository = accountRepository;
            _titleRepository = titleRepository;
            _userXpHistoryRepository = userXpHistoryRepository;
        }

        public async Task<List<Title>> CheckAndUnlockLevelTitlesAsync(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return new List<Title>();
            int currentLevel = LevelEngine.GetLevel(user.TotalXP);
            return await CheckAndUnlockTitlesAsync(userId, TitleRequirementType.Level, (long)currentLevel);
        }

        public async Task<List<Title>> CheckAndUnlockTitlesAsync(string userId, TitleRequirementType type, long value)
        {
            var potentialTitles = await _titleRepository.GetEligibleTitlesAsync(type, value);
            var newlyUnlocked = new List<Title>();
            foreach (var title in potentialTitles)
            {
                bool hasTitle = await _accountRepository.HasTitleAsync(userId, title.TitleId);
                if (!hasTitle)
                {
                    await _accountRepository.AddAccountTitleAsync(new AccountTitle { UserId = userId, TitleId = title.TitleId, EarnedAt = DateTime.Now });
                    newlyUnlocked.Add(title);
                }
            }
            if (newlyUnlocked.Any()) await _accountRepository.SaveChangesAsync(default);
            return newlyUnlocked;
        }

        public async Task<bool> EquipTitleAsync(string userId, string titleId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return false;
            if (!await _accountRepository.HasTitleAsync(userId, titleId)) return false;
            user.CurrentTitleId = titleId;
            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(default);
            return true;
        }

        public async Task<List<Title>> GetUnlockedTitlesAsync(string userId) => await _accountRepository.GetUnlockedTitlesForUserAsync(userId);

        public async Task<(List<(Title title, DateTime earnedAt)> items, int totalCount)> GetUnlockedTitlesWithPagingAsync(string userId, int pageNumber, int pageSize)
        {
            return await _accountRepository.GetUnlockedTitlesWithPagingAsync(userId, pageNumber, pageSize);
        }

        public async Task<List<Title>> CheckAndUnlockDailyTitlesAsync(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return new List<Title>();

            var now = DateTime.Now;
            var allNewlyUnlocked = new List<Title>();

            // 1. Kiểm tra Streak (Chuỗi ngày học liên tiếp tối đa)
            var streakResults = await CheckAndUnlockTitlesAsync(userId, TitleRequirementType.Streak, (long)user.MaxStreak);
            allNewlyUnlocked.AddRange(streakResults);

            // 2. Kiểm tra InactivityDays (Số ngày user không online)
            // Tính số ngày kể từ lần login cuối cùng cho đến nay
            if (user.LastLoginAt.HasValue)
            {
                int inactiveDays = (now.Date - user.LastLoginAt.Value.Date).Days;
                if (inactiveDays > 0)
                {
                    var inactiveResults = await CheckAndUnlockTitlesAsync(userId, TitleRequirementType.InactivityDays, (long)inactiveDays);
                    allNewlyUnlocked.AddRange(inactiveResults);
                }
            }

            // 3. Kiểm tra StudyDaysTotal (Tổng số ngày THỰC TẾ user có hoạt động học)
            // Thay vì dùng (now - CreatedAt), ta đếm số ngày duy nhất có phát sinh XP trong lịch sử.
            int studyDays = await _userXpHistoryRepository.CountActiveDaysAsync(userId);
            var totalDaysResults = await CheckAndUnlockTitlesAsync(userId, TitleRequirementType.StudyDaysTotal, (long)studyDays);
            allNewlyUnlocked.AddRange(totalDaysResults);

            return allNewlyUnlocked.DistinctBy(t => t.TitleId).ToList();
        }
    }
}
