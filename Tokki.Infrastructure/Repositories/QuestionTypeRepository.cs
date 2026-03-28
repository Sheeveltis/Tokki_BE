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
    public class QuestionTypeRepository : IQuestionTypeRepository
    {
        private readonly TokkiDbContext _context;

        public QuestionTypeRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionType?> GetByIdAsync(string questionTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionTypes
                .FirstOrDefaultAsync(qt => qt.QuestionTypeId == questionTypeId, cancellationToken);
        }

        public async Task<IEnumerable<QuestionType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.QuestionTypes
                .Where(qt => qt.IsActive)
                .OrderBy(qt => qt.OrderIndex)
                .ThenBy(qt => qt.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<QuestionType>> GetBySkillAsync(QuestionSkill skill, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionTypes
                .Where(qt => qt.Skill == skill && qt.IsActive)
                .OrderBy(qt => qt.Name)
                .ToListAsync(cancellationToken);
        }
        public async Task<(IEnumerable<QuestionType> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null,
            QuestionSkill? skill = null,
            DifficultyLevel? difficulty = null,
            ExamType? examType = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.QuestionTypes.AsNoTracking().Where(qt => qt.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.Trim();
                query = query.Where(qt => qt.Name.Contains(search) 
                                     || (qt.Code != null && qt.Code.Contains(search))
                                     || qt.QuestionTypeId.Contains(search));
            }

            if (skill.HasValue)
            {
                query = query.Where(qt => qt.Skill == skill.Value);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(qt => qt.Difficulty == difficulty.Value);
            }

            if (examType.HasValue)
            {
                query = query.Where(qt => qt.ExamType == examType.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(qt => qt.OrderIndex)
                .ThenBy(qt => qt.ExamType)
                .ThenBy(qt => qt.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> IsCodeExistsAsync(string code, string? excludeId = null)
        {
            var query = _context.QuestionTypes.Where(qt => qt.Code == code);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(qt => qt.QuestionTypeId != excludeId);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> IsNameExistsAsync(string name, string? excludeId = null)
        {
            var query = _context.QuestionTypes.Where(qt => qt.Name == name);
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(qt => qt.QuestionTypeId != excludeId);
            }
            return await query.AnyAsync();
        }

        public async Task AddAsync(QuestionType questionType)
        {
            await _context.QuestionTypes.AddAsync(questionType);
        }

        public Task UpdateAsync(QuestionType questionType)
        {
            _context.QuestionTypes.Update(questionType);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(QuestionType questionType)
        {
            _context.QuestionTypes.Remove(questionType);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
        public async Task AddRangeAsync(IEnumerable<QuestionType> entities)
        {
            await _context.QuestionTypes.AddRangeAsync(entities);
        }
        public async Task<List<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            if (codes == null || !codes.Any())
            {
                return new List<string>();
            }
            return await _context.QuestionTypes
                .Where(q => codes.Contains(q.Code))
                .Select(q => q.Code)
                .ToListAsync(cancellationToken);
        }
        public async Task<int> GetMaxOrderIndexAsync(CancellationToken cancellationToken = default)
        {
            // Lấy số OrderIndex lớn nhất. Nếu DB rỗng (null) thì trả về 0
            return await _context.QuestionTypes
                .MaxAsync(q => (int?)q.OrderIndex, cancellationToken) ?? 0;
        }
    }
}
