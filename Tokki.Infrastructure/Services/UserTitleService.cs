using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.Common.Helpers;

namespace Tokki.Infrastructure.Services
{
    public class UserTitleService : IUserTitleService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITitleRepository _titleRepository;

        public UserTitleService(IAccountRepository accountRepository, ITitleRepository titleRepository)
        {
            _accountRepository = accountRepository;
            _titleRepository = titleRepository;
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
                    await _accountRepository.AddAccountTitleAsync(new AccountTitle { UserId = userId, TitleId = title.TitleId, EarnedAt = DateTime.UtcNow.AddHours(7) });
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
    }
}
