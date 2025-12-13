
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
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
                .FirstOrDefaultAsync(o => o.OptionId == optionId, cancellationToken);
        }

        public async Task<IEnumerable<QuestionOption>> GetByQuestionBankIdAsync(string questionBankId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionOptions
                .Where(o => o.QuestionBankId == questionBankId)
                .OrderBy(o => o.KeyOption)
                .ToListAsync(cancellationToken);
        }

        public async Task<QuestionOption?> GetCorrectOptionAsync(string questionBankId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionOptions
                .FirstOrDefaultAsync(o => o.QuestionBankId == questionBankId && o.IsCorrect, cancellationToken);
        }

        public async Task AddAsync(QuestionOption questionOption)
        {
            await _context.QuestionOptions.AddAsync(questionOption);
        }

        public async Task AddRangeAsync(IEnumerable<QuestionOption> questionOptions)
        {
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
    }
}
