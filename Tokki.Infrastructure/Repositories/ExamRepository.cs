using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class ExamRepository : IExamRepository
    {
        private readonly TokkiDbContext _context;

        public ExamRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Exam?> GetByIdAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.Exams
                .FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        }

        public async Task<Exam?> GetByIdWithDetailsAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.Exams
                .Include(e => e.ExamTemplate)
                    .ThenInclude(et => et.TemplateParts)
                .Include(e => e.ExamQuestions.OrderBy(eq => eq.QuestionNo))
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionOptions)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionType)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.Passage)
                .FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        }

        public async Task<(IEnumerable<Exam> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamType? type = null,
            ExamStatus? status = null,
            string? examTemplateId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Exams
                .Include(e => e.ExamTemplate)
                .Include(e => e.ExamQuestions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.Title.Contains(searchTerm));
            }

            if (type.HasValue)
            {
                query = query.Where(e => e.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(examTemplateId))
            {
                query = query.Where(e => e.ExamTemplateId == examTemplateId);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> IsTitleExistsAsync(string title, string? excludeId = null)
        {
            var query = _context.Exams.Where(e => e.Title == title);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(e => e.ExamId != excludeId);
            }
            return await query.AnyAsync();
        }

        public async Task<int> GetQuestionCountAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .CountAsync(cancellationToken);
        }

        public async Task AddAsync(Exam exam)
        {
            await _context.Exams.AddAsync(exam);
        }

        public Task UpdateAsync(Exam exam)
        {
            _context.Exams.Update(exam);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Exam exam)
        {
            _context.Exams.Remove(exam);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }

}
