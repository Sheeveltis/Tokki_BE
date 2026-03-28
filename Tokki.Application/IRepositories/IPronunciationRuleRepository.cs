using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IPronunciationRuleRepository
    {
        Task AddAsync(PronunciationRule rule);
        Task UpdateAsync(PronunciationRule rule);
        Task DeleteAsync(PronunciationRule rule);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<PronunciationRule?> GetByIdAsync(string id);
        Task<PronunciationRule?> GetByIdWithDetailsAsync(string id);

        Task<bool> IsRuleNameExistsAsync(string ruleName, string? excludeId = null);
        Task<List<PronunciationRule>> GetAllActiveRulesAsync(CancellationToken cancellationToken = default);
        Task<(List<PronunciationRule> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
    }
}
