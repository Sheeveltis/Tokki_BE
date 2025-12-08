using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums; 
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly TokkiDbContext _context;

        public ReportRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Report> AddAsync(Report report)
        {
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
            return report; 
        }

        public async Task<Report?> GetByIdAsync(string id)
        {
            return await _context.Reports.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        public async Task<List<Report>> GetByUserIdAsync(string userId)
        {
            return await _context.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt) 
                .ToListAsync();
        }

        public async Task<List<Report>> GetUnreadResolvedReportsAsync(string userId)
        {
            return await _context.Reports
                .Where(r => !r.IsDeleted && 
                            r.UserId == userId
                            && r.UserHasRead == false
                            && (r.Status == ReportStatus.Fixed || r.Status == ReportStatus.Rejected)) 
                .OrderByDescending(r => r.ResolvedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Report report)
        {
            _context.Reports.Update(report);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Report report)
        {
            report.IsDeleted = true;
            report.DeletedAt = DateTime.UtcNow;

            _context.Reports.Update(report); 
            await _context.SaveChangesAsync();
        }
        public async Task<List<Report>> GetAllAsync(ReportStatus? status)
        {
            var query = _context.Reports.AsQueryable();

            query = query.Where(r => !r.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            query = query.OrderByDescending(r => r.CreatedAt);

            return await query.ToListAsync();
        }
    }
}