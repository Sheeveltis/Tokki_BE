using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class QuestionBankRepository : IQuestionBankRepository
    {
        private readonly TokkiDbContext _context;

        public QuestionBankRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionBank?> GetByIdAsync(string questionBankId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .FirstOrDefaultAsync(q => q.QuestionBankId == questionBankId, cancellationToken);
        }

        public async Task<QuestionBank?> GetByIdWithDetailsAsync(string questionBankId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .Include(q => q.Passage)
                .Include(q => q.QuestionType)
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(q => q.QuestionBankId == questionBankId, cancellationToken);
        }

        public async Task<(IEnumerable<QuestionBank> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? questionTypeId = null,
            string? passageId = null,
            QuestionBankStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            // Query nền để filter + count (không Include cho nhẹ)
            IQueryable<QuestionBank> baseQuery = _context.QuestionBank.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                baseQuery = baseQuery.Where(q =>
                    (q.Content != null && q.Content.Contains(searchTerm)) ||
                    (q.Explanation != null && q.Explanation.Contains(searchTerm)));
            }

          
         

            if (!string.IsNullOrEmpty(questionTypeId))
            {
                baseQuery = baseQuery.Where(q => q.QuestionTypeId == questionTypeId);
            }

            if (!string.IsNullOrEmpty(passageId))
            {
                baseQuery = baseQuery.Where(q => q.PassageId == passageId);
            }

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(q => q.Status == status.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Query lấy items (Include đầy đủ)
            var items = await baseQuery
                .Include(q => q.Passage)
                .Include(q => q.QuestionType)
                .Include(q => q.QuestionOptions)
                .OrderByDescending(q => q.CreatedAt)
                .ThenByDescending(q => q.QuestionBankId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IEnumerable<QuestionBank>> GetByPassageIdAsync(string passageId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .Include(q => q.QuestionOptions)
                .Where(q => q.PassageId == passageId && q.Status == QuestionBankStatus.Active)
                .OrderByDescending(q => q.CreatedAt)
                .ThenByDescending(q => q.QuestionBankId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QuestionBank>> GetByQuestionTypeIdAsync(string questionTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .Include(q => q.Passage)
                .Include(q => q.QuestionType)
                .Include(q => q.QuestionOptions)
                .Where(q => q.QuestionTypeId == questionTypeId && q.Status == QuestionBankStatus.Active)
                .OrderByDescending(q => q.CreatedAt)
                .ThenByDescending(q => q.QuestionBankId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(QuestionBank questionBank)
        {
            await _context.QuestionBank.AddAsync(questionBank);
        }

        public Task UpdateAsync(QuestionBank questionBank)
        {
            _context.QuestionBank.Update(questionBank);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(QuestionBank questionBank)
        {
            _context.QuestionBank.Remove(questionBank);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
        public async Task<List<QuestionBank>> GetByIdsAsync(IEnumerable<string> questionBankIds, CancellationToken cancellationToken = default)
        {
            var ids = questionBankIds.Distinct().ToList();

            return await _context.QuestionBank
                .Where(q => ids.Contains(q.QuestionBankId))
                .ToListAsync(cancellationToken);
        }

        public Task UpdateRangeAsync(IEnumerable<QuestionBank> questionBanks)
        {
            _context.QuestionBank.UpdateRange(questionBanks);
            return Task.CompletedTask;
        }
        public async Task<bool> AnyUsingPassageAsync(string passageId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .AsNoTracking()
                .AnyAsync(q => q.PassageId == passageId && q.Status != QuestionBankStatus.Deleted, cancellationToken);
        }
    }
}
