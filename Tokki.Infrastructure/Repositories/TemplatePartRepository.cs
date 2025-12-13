using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Tokki.Infrastructure.Repositories
{
    public class TemplatePartRepository : ITemplatePartRepository
    {
        private readonly TokkiDbContext _context;

        public TemplatePartRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<TemplatePart?> GetByIdAsync(string templatePartId, CancellationToken cancellationToken = default)
        {
            return await _context.TemplateParts
                .FirstOrDefaultAsync(tp => tp.TemplatePartId == templatePartId, cancellationToken);
        }

        public async Task<IEnumerable<TemplatePart>> GetByTemplateIdAsync(string examTemplateId, CancellationToken cancellationToken = default)
        {
            return await _context.TemplateParts
                .Where(tp => tp.ExamTemplateId == examTemplateId)
                .OrderBy(tp => tp.QuestionFrom)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsQuestionRangeOverlapAsync(string examTemplateId, int questionFrom, int questionTo, string? excludePartId = null)
        {
            var query = _context.TemplateParts
                .Where(tp => tp.ExamTemplateId == examTemplateId)
                .Where(tp =>
                    (questionFrom >= tp.QuestionFrom && questionFrom <= tp.QuestionTo) ||
                    (questionTo >= tp.QuestionFrom && questionTo <= tp.QuestionTo) ||
                    (questionFrom <= tp.QuestionFrom && questionTo >= tp.QuestionTo)
                );

            if (!string.IsNullOrEmpty(excludePartId))
            {
                query = query.Where(tp => tp.TemplatePartId != excludePartId);
            }

            return await query.AnyAsync();
        }

        public async Task<TemplatePart?> GetPartByQuestionNoAsync(string examTemplateId, int questionNo, CancellationToken cancellationToken = default)
        {
            return await _context.TemplateParts
                .FirstOrDefaultAsync(tp =>
                    tp.ExamTemplateId == examTemplateId &&
                    questionNo >= tp.QuestionFrom &&
                    questionNo <= tp.QuestionTo,
                    cancellationToken);
        }

        public async Task AddAsync(TemplatePart templatePart)
        {
            await _context.TemplateParts.AddAsync(templatePart);
        }

        public async Task AddRangeAsync(IEnumerable<TemplatePart> templateParts)
        {
            await _context.TemplateParts.AddRangeAsync(templateParts);
        }

        public Task UpdateAsync(TemplatePart templatePart)
        {
            _context.TemplateParts.Update(templatePart);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TemplatePart templatePart)
        {
            _context.TemplateParts.Remove(templatePart);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<TemplatePart> templateParts)
        {
            _context.TemplateParts.RemoveRange(templateParts);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
