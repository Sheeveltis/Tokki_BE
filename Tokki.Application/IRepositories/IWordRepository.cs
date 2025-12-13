using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IWordRepository
    {
        Task<Word?> GetByIdAsync(string wordId);
        Task<Word?> GetByTextAsync(string text);
        Task<List<Word>> GetByIdsAsync(List<string> wordIds);
        Task<(List<Word> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            WordStatus? status = null);
        Task<bool> IsWordTextExistsAsync(string text, string? excludeWordId = null);
        Task AddAsync(Word word);
        Task UpdateAsync(Word word);
        Task DeleteAsync(Word word);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<(List<Word> items, int totalCount)> GetPagedWordsByTopicIdAsync(
        string topicId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        WordStatus? status = null
    );

    }
}
