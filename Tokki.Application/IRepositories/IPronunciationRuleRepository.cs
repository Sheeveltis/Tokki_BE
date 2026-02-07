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

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<PronunciationRule?> GetByIdAsync(string id);

        Task<bool> IsRuleNameExistsAsync(string ruleName);
    }
}
