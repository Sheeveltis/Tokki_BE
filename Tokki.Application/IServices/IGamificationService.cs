namespace Tokki.Application.IServices
{
    public interface IGamificationService
    {
        Task CheckLoginGamificationAsync(string userId);

        Task<bool> TrackStudyTimeAsync(string userId, double seconds);
    }
}