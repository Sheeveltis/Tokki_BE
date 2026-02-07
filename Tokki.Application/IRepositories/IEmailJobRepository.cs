using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IEmailJobRepository
    {
        // Create
        Task AddAsync(EmailJob job);

        // Worker
        Task<List<EmailJob>> GetPendingJobsAsync(DateTime scheduledTime);

        // Read
        Task<EmailJob?> GetByIdAsync(string jobId);

        // Get all (paged + filter)
        Task<(List<EmailJob> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            EmailJobStatus? status = null,
            UserTargetGroup? targetGroup = null,
            DateTime? scheduledFrom = null,
            DateTime? scheduledTo = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null,
            string? searchSubject = null,
            bool includeDeleted = false
        );

        // Update
        Task UpdateAsync(EmailJob job);

        // Soft delete
        Task SoftDeleteAsync(EmailJob job);

        // Save
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
