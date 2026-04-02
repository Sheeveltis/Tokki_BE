using Tokki.Domain.Entities;

namespace Tokki.Application.IServices
{
    public interface IGamificationService
    {
        Task CheckLoginGamificationAsync(Account user);
        Task<bool> TrackStudyTimeAsync(string userId, double seconds);
    }
}