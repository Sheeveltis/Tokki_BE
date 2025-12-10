using Tokki.Domain.Entities;

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
    }
}