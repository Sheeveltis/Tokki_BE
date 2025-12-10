using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
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

        // HÀM LƯU SESSION
        public async Task AddSessionAsync(Session session)
        {
            await _context.Session.AddAsync(session);
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

            return await _context.Accounts
                .AnyAsync(u => u.PhoneNumber == phoneNumber);
        }
        public async Task<List<string>> GetEmailsByTargetGroupAsync(UserTargetGroup targetGroup)
        {
            var now = DateTime.UtcNow.AddHours(7);

            // Bắt đầu query từ bảng Account
            var query = _context.Accounts.AsNoTracking()
                .Where(u => u.Status == AccountStatus.Active); // Chỉ lấy user đang hoạt động

            switch (targetGroup)
            {
                case UserTargetGroup.VipUsers:
                    // Logic: Có ít nhất 1 gói Active và còn hạn
                    query = query.Where(u => _context.Subscriptions.Any(s =>
                        s.UserId == u.UserId &&
                        s.Status == SubscriptionStatus.Active &&
                        s.EndDate > now
                    ));
                    break;

                case UserTargetGroup.FreeUsers:
                    // Logic: KHÔNG CÓ gói nào Active và còn hạn
                    query = query.Where(u => !_context.Subscriptions.Any(s =>
                        s.UserId == u.UserId &&
                        s.Status == SubscriptionStatus.Active &&
                        s.EndDate > now
                    ));
                    break;

                case UserTargetGroup.All:
                default:
                    // Không lọc gì thêm
                    break;
            }

            // Chỉ select cột Email để tối ưu hiệu suất
            return await query.Select(u => u.Email).ToListAsync();
        }
        public async Task<bool> IsPhoneNumberUsedByOtherUserAsync(string phoneNumber, string currentUserId)
        {
            // 1. Nếu số điện thoại truyền vào rỗng thì coi như không trùng (bỏ qua)
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            // 2. Tìm kiếm: Tìm user có số điện thoại trùng VÀ user đó KHÔNG phải là người đang sửa.
            return await _context.Accounts
                .AnyAsync(u => u.PhoneNumber == phoneNumber && u.UserId != currentUserId);
        }
    }
}