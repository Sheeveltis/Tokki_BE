using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IMiniGameRepository
    {
        Task<List<Vocabulary>> GetRandomVocabulariesByTopicAsync(string topicId, int quantity, CancellationToken cancellationToken);
        Task<List<Topic>> GetSolitaireTopicsWithVocabsAsync(CancellationToken token = default);
        Task<List<DailyWordle>> GetDailyWordlesByDateAsync(DateOnly date, CancellationToken token = default);
        Task<List<UserWordleProgress>> GetUserWordleProgressAsync(string userId, IEnumerable<string> dailyWordleIds, CancellationToken token = default);
        Task<DailyWordle?> GetDailyWordleByIdAsync(string id, CancellationToken token = default);
        void AddUserWordleProgress(UserWordleProgress progress);
        Task<int> SaveChangesAsync(CancellationToken token = default);

    }
}
