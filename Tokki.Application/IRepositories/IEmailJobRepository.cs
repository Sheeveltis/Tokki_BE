using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IEmailJobRepository
    {
        // Hàm dùng cho Handler (Tạo mới)
        Task AddAsync(EmailJob job);

        // Hàm dùng cho Background Worker (Lấy danh sách cần gửi)
        Task<List<EmailJob>> GetPendingJobsAsync(DateTime scheduledTime);

        // Hàm chung
        Task UpdateAsync(EmailJob job);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}