using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Leaderboard.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IAccountRepository
    {
        Task<bool> IsEmailExistsAsync(string email);
        Task AddAsync(Account user);
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<Account?> GetByEmailAsync(string email);
        Task AddSessionAsync(Session session);
        Task UpdateUserAsync(Account user);
        Task<Account?> GetByIdAsync(string userId);
        Task<bool> IsPhoneNumberExistsAsync(string phoneNumber);
        Task<bool> IsPhoneNumberUsedByOtherUserAsync(string phoneNumber, string currentUserId);

        Task<bool> HasTitleAsync(string userId, string titleId);
        Task AddAccountTitleAsync(AccountTitle accountTitle);
        Task<List<LeaderboardItemDto>> GetLeaderboardAsync(LeaderboardTimeFrame timeFrame, int top);
        Task<(IEnumerable<Account> items, int totalCount)> GetPagedAsync(
              int pageNumber,
              int pageSize
          );

        Task<int> CountUnlockedTitlesAsync(string userId);
        Task<int> CountSessionsAsync(string userId);
        Task<int> CountSocialLoginsAsync(string userId);
        /// <summary>
        /// Kho - hàm của Kho dùng lấy thông tin author vs comment.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<AccountBasicInfoDTO?> GetBasicInfoAsync(string userId);
    }
}