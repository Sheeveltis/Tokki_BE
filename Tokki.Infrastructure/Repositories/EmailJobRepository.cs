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
            // Lấy các Job đang Pending và đã đến giờ gửi
            return await _context.EmailJobs
                .Where(j => j.Status == EmailJobStatus.Pending && j.ScheduledTime <= scheduledTime)
                .ToListAsync();
        }

        public Task UpdateAsync(EmailJob job)
        {
            _context.EmailJobs.Update(job);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}