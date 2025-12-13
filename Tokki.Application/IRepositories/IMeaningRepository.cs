using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IMeaningRepository
    {
        Task<List<Meaning>> GetMeaningsByWordIdAndTopicIdAsync(string wordId, string topicId);
        Task<Meaning?> GetByIdAsync(string meaningId);
        Task<List<Meaning>> GetByWordIdAsync(string wordId);
        Task<List<Meaning>> GetByIdsAsync(List<string> meaningIds);
        Task<(List<Meaning> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            MeaningStatus? status = null);
        Task AddAsync(Meaning meaning);
        Task UpdateAsync(Meaning meaning);
        Task DeleteAsync(Meaning meaning);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Meaning?> GetMeaningByDefinitionAndTopicAsync(
        string wordId,
        string definition,
        string topicId);

        Task<(List<Meaning> items, int totalCount)> GetPagedMeaningsByWordIdAsync(
        string wordId,
        int pageNumber,
        int pageSize,
        string? topicId = null,
        MeaningStatus? status = null
    );
        Task<bool> DeleteByIdAsync(string meaningId); // Xóa cứng by ID
        Task<bool> SoftDeleteAsync(string meaningId, string userId); // Xóa mềm
        Task<bool> ExistsAsync(string meaningId); // Kiểm tra tồn tại

    }
}
