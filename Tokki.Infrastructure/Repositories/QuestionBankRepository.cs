using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Tokki.Domain.Enums;
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
            QuestionSkill? skill = null,
            DifficultyLevel? difficultyLevel = null,
            string? questionTypeId = null,
            string? passageId = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.QuestionBank
                .Include(q => q.Passage)
                .Include(q => q.QuestionType)
                .Include(q => q.QuestionOptions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(q => q.Content!.Contains(searchTerm) || q.Explanation!.Contains(searchTerm));
            }

            if (skill.HasValue)
            {
                query = query.Where(q => q.Skill == skill.Value);
            }

            if (difficultyLevel.HasValue)
            {
                query = query.Where(q => q.DifficultyLevel == difficultyLevel.Value);
            }

            if (!string.IsNullOrEmpty(questionTypeId))
            {
                query = query.Where(q => q.QuestionTypeId == questionTypeId);
            }

            if (!string.IsNullOrEmpty(passageId))
            {
                query = query.Where(q => q.PassageId == passageId);
            }

            if (isActive.HasValue)
            {
                query = query.Where(q => q.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(q => q.QuestionBankId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IEnumerable<QuestionBank>> GetByPassageIdAsync(string passageId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .Include(q => q.QuestionOptions)
                .Where(q => q.PassageId == passageId && q.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QuestionBank>> GetByQuestionTypeIdAsync(string questionTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .Include(q => q.QuestionOptions)
                .Where(q => q.QuestionTypeId == questionTypeId && q.IsActive)
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
    }

}
