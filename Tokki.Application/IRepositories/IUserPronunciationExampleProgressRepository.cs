using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserPronunciationExampleProgressRepository
    {
        Task<UserPronunciationExampleProgress?> GetByUserIdAndExampleIdAsync(string userId, string exampleId);
        Task<List<UserPronunciationExampleProgress>> GetByUserIdAndRuleIdAsync(string userId, string ruleId);
        Task<int> CountPracticedByUserIdAndRuleIdAsync(string userId, string ruleId);
        Task AddAsync(UserPronunciationExampleProgress progress);
        void Update(UserPronunciationExampleProgress progress);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
