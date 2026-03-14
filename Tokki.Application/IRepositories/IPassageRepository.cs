using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IPassageRepository
    {
        Task<Passage?> GetByIdAsync(string passageId, CancellationToken cancellationToken = default);

        Task<(IEnumerable<Passage> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            PassageMediaType? mediaType = null,
            PassageStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<bool> IsTitleExistsAsync(string title, string? excludeId = null);

        Task AddAsync(Passage passage);
        Task UpdateAsync(Passage passage);
        Task DeleteAsync(Passage passage);

        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Kho - dùng thêm đoạn văn hàng loạt
        /// Chủ yếu là dùng bên excel
        /// </summary>
        /// <param name="passages"></param>
        /// <returns></returns>
        Task AddRangeAsync(IEnumerable<Passage> passages);
        Task<List<Passage>> GetByIdsAsync(List<string> ids, CancellationToken token);
    }
}
