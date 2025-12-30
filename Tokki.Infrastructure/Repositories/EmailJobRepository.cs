using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class EmailJobRepository : IEmailJobRepository
    {
        private readonly TokkiDbContext _context;

        public EmailJobRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EmailJob job)
        {
            await _context.EmailJobs.AddAsync(job);
        }

        public async Task<List<EmailJob>> GetPendingJobsAsync(DateTime scheduledTime)
        {
            return await _context.EmailJobs
                .Where(j => j.Status == EmailJobStatus.Pending && j.ScheduledTime <= scheduledTime)
                .OrderBy(j => j.ScheduledTime)
                .ToListAsync();
        }

        public async Task<EmailJob?> GetByIdAsync(string jobId)
        {
            return await _context.EmailJobs
                .FirstOrDefaultAsync(x => x.JobId == jobId);
        }

        public async Task<(List<EmailJob> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            EmailJobStatus? status = null,
            UserTargetGroup? targetGroup = null,
            DateTime? scheduledFrom = null,
            DateTime? scheduledTo = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null,
            string? searchSubject = null,
            bool includeDeleted = false)
        {
            var query = _context.EmailJobs.AsNoTracking().AsQueryable();

            if (!includeDeleted)
                query = query.Where(x => x.Status != EmailJobStatus.Deleted);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (targetGroup.HasValue)
                query = query.Where(x => x.TargetGroup == targetGroup.Value);

            if (scheduledFrom.HasValue)
                query = query.Where(x => x.ScheduledTime >= scheduledFrom.Value);

            if (scheduledTo.HasValue)
                query = query.Where(x => x.ScheduledTime <= scheduledTo.Value);

            if (createdFrom.HasValue)
                query = query.Where(x => x.CreatedAt >= createdFrom.Value);

            if (createdTo.HasValue)
                query = query.Where(x => x.CreatedAt <= createdTo.Value);

            if (!string.IsNullOrWhiteSpace(searchSubject))
            {
                var s = searchSubject.Trim().ToLower();
                query = query.Where(x => x.Subject != null && x.Subject.ToLower().Contains(s));
            }

            // Sort: mới nhất lên đầu
            query = query.OrderByDescending(x => x.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task UpdateAsync(EmailJob job)
        {
            _context.EmailJobs.Update(job);
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(EmailJob job)
        {
            job.Status = EmailJobStatus.Deleted;
            _context.EmailJobs.Update(job);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
