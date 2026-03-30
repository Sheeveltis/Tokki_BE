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

        /// <summary>
        /// Tìm kiếm & phân trang account theo một ô SearchText duy nhất
        /// (match theo UserId, FullName, Email, hoặc PhoneNumber).
        /// Không ảnh hưởng tới GetPagedAsync cũ.
        /// </summary>
        Task<(IEnumerable<Account> items, int totalCount)> GetPagedWithSearchAsync(
            int pageNumber,
            int pageSize,
            string? searchText,
            AccountStatus? status,
            AccountRole? role,
            VipStatus? vipStatus);

        Task<int> CountUnlockedTitlesAsync(string userId);
        Task<int> CountSocialLoginsAsync(string userId);
        /// <summary>
        /// Kho - hàm của Kho dùng lấy thông tin author vs comment.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<AccountBasicInfoDTO?> GetBasicInfoAsync(string userId);
        Task<Dictionary<string, AccountBasicInfoDTO>> GetBasicInfosAsync(List<string> userIds);
        Task<List<string>> GetExistingEmailsAsync(List<string> emails, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<Account> accounts, CancellationToken cancellationToken = default);
        Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}