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
        Task<WordleSentenceSubmission?> GetWordleSubmissionByIdAsync(string submissionId, CancellationToken token);
        Task<List<WordleSentenceSubmission>> GetTopPublicSentencesAsync(string dailyWordleId, int top, CancellationToken token);
        Task<WordleSentenceLike?> GetLikeAsync(string userId, string submissionId, CancellationToken token);
        void AddLike(WordleSentenceLike like);
        void RemoveLike(WordleSentenceLike like);
        Task<List<UserWordleProgress>> GetWordlePlayersAsync(string dailyWordleId, CancellationToken token);
        Task<List<WordleSentenceSubmission>> GetWordleLeaderboardAsync(string dailyWordleId, CancellationToken token, bool includePrivate = false);
    }
}
