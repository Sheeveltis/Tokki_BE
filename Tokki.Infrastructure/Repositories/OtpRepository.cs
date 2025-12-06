// Tokki.Infrastructure/Repositories/OtpRepository.cs

using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly TokkiDbContext _context; // Thay bằng tên DbContext thực tế

        public OtpRepository(TokkiDbContext context)
        {
            _context = context;
        }

        // 1. Thêm OTP mới vào DbContext
        public async Task AddAsync(Otp otp)
        {
            await _context.OtpCodes.AddAsync(otp);
        }

        // 2. Lấy mã OTP HỢP LỆ mới nhất
        public async Task<Otp?> GetLatestValidOtpAsync(string email, OtpType type)
        {
            // Tìm mã OTP thỏa mãn các điều kiện:
            // 1. Đúng Email
            // 2. Đúng Type (Login, Register...)
            // 3. Chưa sử dụng (IsUsed == false)
            // 4. Chưa hết hạn (ExpiresAt > DateTime.UtcNow)
            // 5. Lấy bản ghi mới nhất (OrderByDescending(o => o.CreatedAt))

            var latestOtp = await _context.OtpCodes
                .Where(o =>
                    o.Email == email &&
                    o.Type == type &&
                    o.IsUsed == false &&
                    o.ExpiredAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            return latestOtp;
        }

        // 3. Lưu tất cả thay đổi trong DbContext xuống Database
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}