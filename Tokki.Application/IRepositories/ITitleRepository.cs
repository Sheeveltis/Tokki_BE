using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface ITitleRepository
    {
        Task<Title?> GetTitleByNameAsync(string name);
        Task<Title?> GetTitleByIdAsync(int id);
        Task<Title?> GetTitleByXpAsync(long xp); 
    }
}