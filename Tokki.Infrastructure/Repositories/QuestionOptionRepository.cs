using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tokki.Infrastructure.Repositories
{
    public class QuestionOptionRepository : IQuestionOptionRepository
    {
        private readonly TokkiDbContext _context;

        public QuestionOptionRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionOption?> GetByIdAsync(string optionId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionOptions
                .FirstOrDefaultAsync(o => o.OptionId == optionId && o.Status == QuestionOptionStatus.Active, cancellationToken);
        }

        public async Task<IEnumerable<QuestionOption>> GetByQuestionBankIdAsync(string questionBankId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionOptions
                .Where(o => o.QuestionBankId == questionBankId && o.Status == QuestionOptionStatus.Active)
                .OrderBy(o => o.KeyOption)
                .ToListAsync(cancellationToken);
        }

        public async Task<QuestionOption?> GetCorrectOptionAsync(string questionBankId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionOptions
                .FirstOrDefaultAsync(o => o.QuestionBankId == questionBankId && o.IsCorrect && o.Status == QuestionOptionStatus.Active, cancellationToken);
        }

        public async Task AddAsync(QuestionOption questionOption)
        {
            questionOption.Status = QuestionOptionStatus.Active; // ✅ Sửa: Gán trực tiếp enum
            await _context.QuestionOptions.AddAsync(questionOption);
        }

        public async Task AddRangeAsync(IEnumerable<QuestionOption> questionOptions)
        {
            foreach (var option in questionOptions)
            {
                option.Status = QuestionOptionStatus.Active; // ✅ Sửa: Gán trực tiếp enum
            }
            await _context.QuestionOptions.AddRangeAsync(questionOptions);
        }

        public Task UpdateAsync(QuestionOption questionOption)
        {
            _context.QuestionOptions.Update(questionOption);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(QuestionOption questionOption)
        {
            _context.QuestionOptions.Remove(questionOption);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<QuestionOption> questionOptions)
        {
            _context.QuestionOptions.RemoveRange(questionOptions);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task SoftDeleteAsync(QuestionOption option)
        {
            option.Status = QuestionOptionStatus.Deleted; 
            _context.QuestionOptions.Update(option);
            await Task.CompletedTask;
        }

        public async Task SoftDeleteRangeAsync(List<QuestionOption> options)
        {
            foreach (var option in options)
            {
                option.Status = QuestionOptionStatus.Deleted; 
            }
            _context.QuestionOptions.UpdateRange(options);
            await Task.CompletedTask;
        }
    }
}