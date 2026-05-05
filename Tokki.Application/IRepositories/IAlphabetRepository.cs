using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IAlphabetRepository
    {
        Task<List<AlphabetData>> GetAllAsync();
        Task<AlphabetData?> GetByIdAsync(int id);
        Task<AlphabetData?> GetByLetterAsync(string letter);
        Task AddAsync(AlphabetData entity);
        Task AddRangeAsync(IEnumerable<AlphabetData> entities);
        Task UpdateAsync(AlphabetData entity);
        Task DeleteAsync(AlphabetData entity);
        Task<(List<AlphabetData> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            AlphabetType? type = null,
            bool? isActive = null);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
