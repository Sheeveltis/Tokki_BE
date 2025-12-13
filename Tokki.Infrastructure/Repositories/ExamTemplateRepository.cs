using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Tokki.Infrastructure.Repositories
{
    public class ExamTemplateRepository : IExamTemplateRepository
    {
        private readonly TokkiDbContext _context;

        public ExamTemplateRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<ExamTemplate?> GetByIdAsync(string examTemplateId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamTemplates
                .FirstOrDefaultAsync(et => et.ExamTemplateId == examTemplateId, cancellationToken);
        }

        public async Task<ExamTemplate?> GetByIdWithPartsAsync(string examTemplateId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamTemplates
                .Include(et => et.TemplateParts.OrderBy(tp => tp.QuestionFrom))
                .FirstOrDefaultAsync(et => et.ExamTemplateId == examTemplateId, cancellationToken);
        }

        public async Task<(IEnumerable<ExamTemplate> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamTemplateStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ExamTemplates
                .Include(et => et.TemplateParts)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(et => et.Name.Contains(searchTerm) || et.Description!.Contains(searchTerm));
            }

            if (status.HasValue)
            {
                query = query.Where(et => et.Status == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(et => et.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> IsNameExistsAsync(string name, string? excludeId = null)
        {
            var query = _context.ExamTemplates.Where(et => et.Name == name);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(et => et.ExamTemplateId != excludeId);
            }
            return await query.AnyAsync();
        }

        public async Task AddAsync(ExamTemplate examTemplate)
        {
            await _context.ExamTemplates.AddAsync(examTemplate);
        }

        public Task UpdateAsync(ExamTemplate examTemplate)
        {
            _context.ExamTemplates.Update(examTemplate);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ExamTemplate examTemplate)
        {
            _context.ExamTemplates.Remove(examTemplate);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
