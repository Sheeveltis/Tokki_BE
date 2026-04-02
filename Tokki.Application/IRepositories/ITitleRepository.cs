using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface ITitleRepository
    {
        Task<Title?> GetTitleByNameAsync(string name, TitleStatus? status = null);
        Task<Title?> GetTitleByIdAsync(string id);
        Task<List<Title>> GetAllTitlesAsync(bool includeInactive = false);
        Task AddAsync(Title title);
        Task UpdateAsync(Title title);
        
        // --- NEW FLEXIBLE LOGIC ---
        Task<List<Title>> GetEligibleTitlesAsync(TitleRequirementType type, long quantity);
        // ---------------------------

        Task<(List<Title> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            TitleStatus? status = null,
            TitleRequirementType? requirementType = null,
            CancellationToken cancellationToken = default);
    }
}