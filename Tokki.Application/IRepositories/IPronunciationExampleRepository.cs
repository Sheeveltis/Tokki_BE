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
        Task AddRangeAsync(IEnumerable<PronunciationExample> entities);
        Task<PronunciationExample?> GetByIdAsync(string id);
        Task<List<PronunciationExample>> GetExamplesByRuleIdAsync(string ruleId, CancellationToken cancellationToken = default);
        Task<PronunciationExample?> GetDetailByIdAsync(string exampleId, CancellationToken cancellationToken = default);
    }
}
