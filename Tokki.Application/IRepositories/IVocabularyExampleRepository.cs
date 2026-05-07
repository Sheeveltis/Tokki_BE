using Microsoft.EntityFrameworkCore.Storage;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IVocabularyExampleRepository
    {
        Task<VocabularyExample?> GetByIdAsync(string exampleId);
        Task<List<VocabularyExample>> GetByVocabularyIdAsync(string vocabularyId);
        Task<VocabularyExample?> GetBySentenceAsync(string vocabularyId, string sentence);
        Task AddAsync(VocabularyExample example);
        Task AddRangeAsync(List<VocabularyExample> examples);
        Task UpdateAsync(VocabularyExample example);
        Task DeleteAsync(VocabularyExample example);
        Task DeleteRangeAsync(List<VocabularyExample> examples);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task<List<VocabularyExample>> GetByVocabularyIdAsync(
       string vocabularyId,
       VocabularyExampleStatus? status,
       CancellationToken cancellationToken);
        IExecutionStrategy CreateExecutionStrategy();
    }
}