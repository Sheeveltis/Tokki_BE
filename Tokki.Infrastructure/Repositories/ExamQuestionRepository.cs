using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class ExamQuestionRepository : IExamQuestionRepository
    {
        private readonly TokkiDbContext _context;

        public ExamQuestionRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<ExamQuestion?> GetByIdAsync(string examQuestionId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamQuestions
                .FirstOrDefaultAsync(eq => eq.ExamQuestionId == examQuestionId, cancellationToken);
        }

        public async Task<ExamQuestion?> GetByExamAndQuestionNoAsync(string examId, int questionNo, CancellationToken cancellationToken = default)
        {
            return await _context.ExamQuestions
                .Include(eq => eq.QuestionBank)
                .FirstOrDefaultAsync(eq => eq.ExamId == examId && eq.QuestionNo == questionNo, cancellationToken);
        }

        public async Task<IEnumerable<ExamQuestion>> GetByExamIdAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .OrderBy(eq => eq.QuestionNo)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ExamQuestion>> GetByExamIdWithDetailsAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamQuestions
                .Include(eq => eq.QuestionBank)
                    .ThenInclude(qb => qb.QuestionOptions)
                .Include(eq => eq.QuestionBank)
                    .ThenInclude(qb => qb.QuestionType)
                .Include(eq => eq.QuestionBank)
                    .ThenInclude(qb => qb.Passage)
                .Where(eq => eq.ExamId == examId)
                .OrderBy(eq => eq.QuestionNo)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsQuestionNoExistsAsync(string examId, int questionNo, string? excludeId = null)
        {
            var query = _context.ExamQuestions
                .Where(eq => eq.ExamId == examId && eq.QuestionNo == questionNo);

            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(eq => eq.ExamQuestionId != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<int> GetMaxQuestionNoAsync(string examId, CancellationToken cancellationToken = default)
        {
            var maxNo = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .MaxAsync(eq => (int?)eq.QuestionNo, cancellationToken);

            return maxNo ?? 0;
        }

        public async Task AddAsync(ExamQuestion examQuestion)
        {
            await _context.ExamQuestions.AddAsync(examQuestion);
        }

        public async Task AddRangeAsync(IEnumerable<ExamQuestion> examQuestions)
        {
            await _context.ExamQuestions.AddRangeAsync(examQuestions);
        }

        public Task UpdateAsync(ExamQuestion examQuestion)
        {
            _context.ExamQuestions.Update(examQuestion);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ExamQuestion examQuestion)
        {
            _context.ExamQuestions.Remove(examQuestion);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<ExamQuestion> examQuestions)
        {
            _context.ExamQuestions.RemoveRange(examQuestions);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
