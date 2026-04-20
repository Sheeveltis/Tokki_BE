using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IPronunciationExampleRepository
    {
        Task AddAsync(PronunciationExample entity);
        Task UpdateAsync(PronunciationExample entity);
        Task DeleteAsync(PronunciationExample entity);
        Task<List<PronunciationExample>> GetAllAsync(CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<PronunciationExample> entities);
        Task<PronunciationExample?> GetByIdAsync(string id);
        Task<List<PronunciationExample>> GetExamplesByRuleIdAsync(string ruleId, CancellationToken cancellationToken = default);
        Task<PronunciationExample?> GetDetailByIdAsync(string exampleId, CancellationToken cancellationToken = default);
        Task<(List<PronunciationExample> Items, int TotalCount)> GetPagedAsync(
            string ruleId,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            Tokki.Domain.Enums.PronunciationDifficulty? difficulty,
            CancellationToken cancellationToken = default);
        Task<int> GetMaxSortOrderAsync(string ruleId, CancellationToken cancellationToken = default);
    }
}
