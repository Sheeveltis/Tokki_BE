using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

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
                .ThenInclude(tp => tp.QuestionType) 
                .FirstOrDefaultAsync(et => et.ExamTemplateId == examTemplateId, cancellationToken);
        }

        public async Task<(IEnumerable<ExamTemplate> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamTemplateStatus? status = null,
            CancellationToken cancellationToken = default,
            ExamType? type = null,
            ExamCreatorFilter? creatorFilter = ExamCreatorFilter.All)
        {
            var query = _context.ExamTemplates
                .Include(et => et.TemplateParts)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                query = query.Where(et =>
                    et.Name.Contains(term) ||
                    (et.Description != null && et.Description.Contains(term)) ||
                    et.ExamTemplateId.Contains(term)
                );
            }

            if (status.HasValue)
            {
                query = query.Where(et => et.Status == status.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(et => et.Type == type.Value);
            }

            if (creatorFilter == ExamCreatorFilter.AI)
            {
                query = query.Where(et => et.CreatedBy == "AI_EXAM_SYSTEM");
            }
            else if (creatorFilter == ExamCreatorFilter.Human)
            {
                query = query.Where(et => et.CreatedBy != "AI_EXAM_SYSTEM");
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
            var query = _context.ExamTemplates.Where(et => et.Name == name && et.Status != ExamTemplateStatus.Deleted);
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
        public async Task<bool> HasExamsAsync(string examTemplateId, CancellationToken cancellationToken = default)
        {
            return await _context.Exams.AnyAsync(e => e.ExamTemplateId == examTemplateId, cancellationToken);
        }
    }
}