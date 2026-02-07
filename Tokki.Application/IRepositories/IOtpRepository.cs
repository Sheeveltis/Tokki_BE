// IOtpRepository.cs
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IOtpRepository
    {
        Task<Otp?> GetByIdAsync(string id); // ✅ Đổi long → string
        Task<Otp?> GetLatestValidOtpAsync(string email, OtpType type);
        Task AddAsync(Otp otp);
        Task UpdateAsync(Otp otp);
        Task DeleteAsync(string id); // ✅ Đổi long → string (nếu có)
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}