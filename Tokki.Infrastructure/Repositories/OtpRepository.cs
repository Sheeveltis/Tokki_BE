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

        public async Task<Otp?> GetLatestValidOtpAsync(string email, OtpType type)
        {
            var latestOtp = await _context.OtpCodes
                .Where(o =>
                    o.Email == email &&
                    o.Type == type &&
                    o.Status == OtpStatus.Active) // 1. Chỉ lấy mã đang Active (thay cho IsUsed)
                                                  // 2. Không check ngày hết hạn ở đây, để Handler check và update Status
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            return latestOtp;
        }
        // Lưu tất cả thay đổi trong DbContext xuống Database
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        public Task UpdateAsync(Otp otp)
        {
            // Hàm Update của EF Core không có Async, nên ta gọi thường
            _context.OtpCodes.Update(otp);

            // Trả về Task hoàn thành để khớp với keyword 'await' bên Handler
            return Task.CompletedTask;
        }
    }
}