using Tokki.Domain.Entities;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Gamification.DTOs;

namespace Tokki.Application.IServices
{
    public interface IGamificationService
    {
        Task CheckLoginGamificationAsync(Account user);
        Task<bool> TrackStudyTimeAsync(string userId, double seconds);
        Task<StreakStatusDto> GetStreakStatusAsync(string userId);
    }
}