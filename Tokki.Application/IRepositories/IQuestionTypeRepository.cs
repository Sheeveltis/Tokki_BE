using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IQuestionTypeRepository
    {
        Task<QuestionType?> GetByIdAsync(string questionTypeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<QuestionType>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<QuestionType>> GetAsync(
            string? keyword = null,
            QuestionSkill? skill = null,
            DifficultyLevel? difficulty = null,
            ExamType? examType = null,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<QuestionType>> GetBySkillAsync(QuestionSkill skill, CancellationToken cancellationToken = default);
        Task<bool> IsCodeExistsAsync(string code, string? excludeId = null);
        Task<bool> IsNameExistsAsync(string name, string? excludeId = null);
        Task AddAsync(QuestionType questionType);
        Task UpdateAsync(QuestionType questionType);
        Task DeleteAsync(QuestionType questionType);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<QuestionType> entities);
        Task<List<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
        Task<int> GetMaxOrderIndexAsync(CancellationToken cancellationToken = default);
    }
}
