using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserPronunciationProgressRepository
    {
        Task<UserPronunciationProgress?> GetByUserIdAndRuleIdAsync(string userId, string ruleId);
        Task<List<UserPronunciationProgress>> GetByUserIdAndRuleIdsAsync(string userId, List<string> ruleIds);
        Task AddAsync(UserPronunciationProgress progress);
        void Update(UserPronunciationProgress progress);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
