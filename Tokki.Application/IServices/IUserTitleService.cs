using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IServices
{
    public interface IUserTitleService
    {
        Task<List<Title>> CheckAndUnlockLevelTitlesAsync(string userId);
        Task<List<Title>> CheckAndUnlockTitlesAsync(string userId, TitleRequirementType type, long value);
        Task<bool> EquipTitleAsync(string userId, string titleId);
        Task<List<Title>> GetUnlockedTitlesAsync(string userId);
        Task<(List<(Title title, DateTime earnedAt)> items, int totalCount)> GetUnlockedTitlesWithPagingAsync(string userId, int pageNumber, int pageSize);
        
        // --- NEW ---
        Task<List<Title>> CheckAndUnlockDailyTitlesAsync(string userId);
    }
}
