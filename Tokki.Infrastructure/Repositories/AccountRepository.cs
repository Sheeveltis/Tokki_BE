using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Leaderboard.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly TokkiDbContext _context;
        public AccountRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            // Kiểm tra email không phân biệt hoa thường
            return await _context.Accounts
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task AddAsync(Account user)
        {
            await _context.Accounts.AddAsync(user);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        // HÀM LẤY USER
        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(u => u.Email == email);
        }

       
        public Task UpdateUserAsync(Account user)
        {
            // Đánh dấu Entity là đã chỉnh sửa
            _context.Accounts.Update(user);

            // Vì EF Core Update không có Async, ta trả về Task hoàn thành 
            // để khớp với Interface trả về Task
            return Task.CompletedTask;
        }
        public async Task<Account?> GetByIdAsync(string userId)
        {
            return await _context.Accounts.FirstOrDefaultAsync(u => u.UserId == userId);
        }
        // Tokki.Infrastructure/Repositories/AccountRepository.cs
        public async Task<bool> IsPhoneNumberExistsAsync(string phoneNumber)
        {
            // Nếu số điện thoại truyền vào rỗng thì coi như không trùng (bỏ qua)
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            return await _context.Accounts.AnyAsync(u => u.PhoneNumber == phoneNumber);
        }
       public async Task<bool> IsPhoneNumberUsedByOtherUserAsync(string phoneNumber, string currentUserId)
        {
            // 1. Nếu số điện thoại truyền vào rỗng thì coi như không trùng (bỏ qua)
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            // 2. Tìm kiếm: Tìm user có số điện thoại trùng VÀ user đó KHÔNG phải là người đang sửa.
            return await _context.Accounts
                .AnyAsync(u => u.PhoneNumber == phoneNumber && u.UserId != currentUserId);
        }

        public async Task<bool> HasTitleAsync(string userId, string titleId)
        {
            return await _context.AccountTitles
                .AnyAsync(at => at.UserId == userId && at.TitleId == titleId);
        }

        public async Task AddAccountTitleAsync(AccountTitle accountTitle)
        {
            await _context.AccountTitles.AddAsync(accountTitle);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LeaderboardItemDto>> GetLeaderboardAsync(LeaderboardTimeFrame timeFrame, int top)
        {
            var result = new List<LeaderboardItemDto>();

            if (timeFrame == LeaderboardTimeFrame.AllTime)
            {
                var users = await _context.Accounts
                    .AsNoTracking()
                    .Where(u => u.Status == Domain.Enums.AccountStatus.Active)
                    .OrderByDescending(u => u.TotalXP)
                    .Take(top)
                    .Include(u => u.CurrentTitle)
                    .Select(u => new LeaderboardItemDto
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        AvatarUrl = u.AvatarUrl,
                        TotalXP = u.TotalXP,
                        TitleName = u.CurrentTitle != null ? u.CurrentTitle.Name : "Chưa có",
                        TitleColor = u.CurrentTitle != null ? u.CurrentTitle.ColorHex : "#000000"
                    })
                    .ToListAsync();

                for (int i = 0; i < users.Count; i++) users[i].Rank = i + 1;
                return users;
            }

            var now = DateTime.UtcNow.AddHours(7);
            DateTime startDate;

            switch (timeFrame)
            {
                case LeaderboardTimeFrame.Day:
                    startDate = now.Date;
                    break;
                case LeaderboardTimeFrame.Week: 
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDate = now.Date.AddDays(-1 * diff);
                    break;
                case LeaderboardTimeFrame.Month:
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                default:
                    startDate = now.Date;
                    break;
            }

            var rankingData = await _context.UserXpHistories
                .Where(h => h.CreatedAt >= startDate)
                .GroupBy(h => h.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Score = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Score)
                .Take(top)
                .ToListAsync();

            if (!rankingData.Any()) return new List<LeaderboardItemDto>();

            var userIds = rankingData.Select(r => r.UserId).ToList();
            var userInfos = await _context.Accounts
                .AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .Include(u => u.CurrentTitle)
                .ToDictionaryAsync(u => u.UserId, u => u);

            int rank = 1;
            foreach (var item in rankingData)
            {
                if (userInfos.TryGetValue(item.UserId, out var user))
                {
                    result.Add(new LeaderboardItemDto
                    {
                        Rank = rank++,
                        UserId = user.UserId,
                        FullName = user.FullName,
                        AvatarUrl = user.AvatarUrl,
                        TotalXP = item.Score,
                        TitleName = user.CurrentTitle?.Name ?? "Chưa có",
                        TitleColor = user.CurrentTitle?.ColorHex ?? "#000000"
                    });
                }
            }
            return result;
        }
        public async Task<(IEnumerable<Account> items, int totalCount)> GetPagedAsync(
           int pageNumber,
           int pageSize)
        {
            var query = _context.Accounts
                .Include(a => a.CurrentTitle)
                .AsQueryable();

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and get items
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Account> items, int totalCount)> GetPagedWithSearchAsync(
            int pageNumber,
            int pageSize,
            string? searchText,
            AccountStatus? status,
            AccountRole? role,
            VipStatus? vipStatus)
        {
            var query = _context.Accounts
                .Include(a => a.CurrentTitle)
                .AsNoTracking()
                .AsQueryable();

            // Search: khớp bất kỳ trường nào (OR) – case insensitive
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var keyword = searchText.Trim().ToLower();
                query = query.Where(a =>
                    a.UserId.ToLower().Contains(keyword) ||
                    (a.FullName != null && a.FullName.ToLower().Contains(keyword)) ||
                    a.Email.ToLower().Contains(keyword) ||
                    (a.PhoneNumber != null && a.PhoneNumber.Contains(keyword)));
            }

            // Filter by Status
            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            // Filter by Role
            if (role.HasValue)
                query = query.Where(a => a.Role == role.Value);

            // Filter by VipStatus
            if (vipStatus.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                query = vipStatus.Value switch
                {
                    VipStatus.Active  => query.Where(a => a.VipExpirationDate.HasValue && a.VipExpirationDate.Value > now),
                    VipStatus.NoVip   => query.Where(a => !a.VipExpirationDate.HasValue),
                    _                 => query
                };
            }

            // Sort mới nhất lên trước
            query = query.OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int> CountUnlockedTitlesAsync(string userId)
        {
            return await _context.AccountTitles
                .CountAsync(at => at.UserId == userId);
        }

        
        public async Task<int> CountSocialLoginsAsync(string userId)
        {
            return await _context.SocialLogins
                .CountAsync(sl => sl.UserId == userId);
        }
        public async Task<AccountBasicInfoDTO?> GetBasicInfoAsync(string userId)
        {
            var query = from a in _context.Accounts
                        where a.UserId == userId
                        join t in _context.Titles
                        on a.CurrentTitleId equals t.TitleId into userTitles
                        from title in userTitles.DefaultIfEmpty()

                        select new AccountBasicInfoDTO
                        {
                            FullName = a.FullName,
                            AvatarUrl = a.AvatarUrl,
                            CurrentTitleName = title != null ? title.Name : null,
                            CurrentColorHexTitle = title != null ? title.ColorHex : null,
                            TitleIconUrl = title != null ? title.IconUrl : null
                        };
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<Dictionary<string, AccountBasicInfoDTO>> GetBasicInfosAsync(List<string> userIds)
        {
            if (userIds == null || !userIds.Any()) return new Dictionary<string, AccountBasicInfoDTO>();

            var query = from a in _context.Accounts
                        where userIds.Contains(a.UserId)
                        join t in _context.Titles
                        on a.CurrentTitleId equals t.TitleId into userTitles
                        from title in userTitles.DefaultIfEmpty()

                        select new
                        {
                            a.UserId,
                            DTO = new AccountBasicInfoDTO
                            {
                                FullName = a.FullName,
                                AvatarUrl = a.AvatarUrl,
                                CurrentTitleName = title != null ? title.Name : null,
                                CurrentColorHexTitle = title != null ? title.ColorHex : null,
                                TitleIconUrl = title != null ? title.IconUrl : null
                            }
                        };

            var items = await query.AsNoTracking().ToListAsync();
            return items.ToDictionary(x => x.UserId, x => x.DTO);
        }

        public async Task AddRangeAsync(IEnumerable<Account> accounts, CancellationToken cancellationToken = default)
        {
            if (accounts == null || !accounts.Any()) return;
            await _context.Accounts.AddRangeAsync(accounts, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<List<string>> GetExistingEmailsAsync(List<string> emails, CancellationToken cancellationToken = default)
        {
            if (emails == null || !emails.Any())
            {
                return new List<string>();
            }

            return await _context.Accounts
                .AsNoTracking() 
                .Where(a => emails.Contains(a.Email))
                .Select(a => a.Email)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<(Title title, DateTime earnedAt)> items, int totalCount)> GetUnlockedTitlesWithPagingAsync(string userId, int pageNumber, int pageSize)
        {
            var query = from at in _context.AccountTitles
                        join t in _context.Titles on at.TitleId equals t.TitleId
                        where at.UserId == userId
                        select new { Title = t, EarnedAt = at.EarnedAt };

            int totalCount = await query.CountAsync();
            var results = await query
                .OrderByDescending(x => x.EarnedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = results.Select(x => (x.Title, x.EarnedAt)).ToList();

            return (items, totalCount);
        }

        public async Task<List<Title>> GetUnlockedTitlesForUserAsync(string userId)
        {
            return await (from at in _context.AccountTitles
                          join t in _context.Titles on at.TitleId equals t.TitleId
                          where at.UserId == userId
                          orderby at.EarnedAt descending
                          select t).ToListAsync();
        }
        public async Task<List<Title>> GetUnlockedTitlesEarnedOnDateAsync(string userId, TitleRequirementType type, DateTime date)
        {
            var today = date.Date;
            return await (from at in _context.AccountTitles
                          join t in _context.Titles on at.TitleId equals t.TitleId
                          where at.UserId == userId 
                             && at.EarnedAt.Date == today
                             && t.RequirementType == type
                          select t).ToListAsync();
        }
    }
}
