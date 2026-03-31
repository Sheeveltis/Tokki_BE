using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface ITitleRepository
    {
        Task<Title?> GetTitleByNameAsync(string name);
        Task<Title?> GetTitleByIdAsync(string id);
        Task<Title?> GetTitleByXpAsync(long xp); 
        Task<List<Title>> GetAllTitlesAsync(bool includeInactive = false);
        Task AddAsync(Title title);
        Task UpdateAsync(Title title);
        Task<(List<Title> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            TitleStatus? status = null,
            CancellationToken cancellationToken = default);
    }
}