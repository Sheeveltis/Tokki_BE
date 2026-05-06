using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IWordleRepository
    {
        Task<DailyWordle?> GetDailyWordleWithVocabAsync(string dailyWordleId, CancellationToken token);

        Task AddSubmissionAsync(WordleSentenceSubmission submission, CancellationToken token);
        Task<WordleSentenceSubmission?> GetSubmissionByIdAsync(string submissionId, CancellationToken token);
        Task UpdateSubmissionAsync(WordleSentenceSubmission submission, CancellationToken token);

        Task<(List<(DailyWordle Item, bool IsLocked)> Items, int TotalCount)> GetPagedDailyWordlesAsync(int pageNumber, int pageSize, DateOnly? date, WordleLevel? level, string? searchTerm, CancellationToken token);
        Task UpdateDailyWordleAsync(DailyWordle dailyWordle, CancellationToken token);
        Task<DailyWordle?> GetDailyWordleByIdAsync(string dailyWordleId, CancellationToken token);
        Task<Tokki.Domain.Entities.Vocabulary?> GetRandomVocabularyByLengthAsync(int length, CancellationToken token);
        Task<Tokki.Domain.Entities.Vocabulary?> GetVocabularyByIdAsync(string vocabularyId, CancellationToken token);
        Task<(List<Tokki.Domain.Entities.Vocabulary> Items, int TotalCount)> GetPagedSuitableVocabsAsync(int length, int pageNumber, int pageSize, string? searchTerm, CancellationToken token);
        Task<bool> AnyUserProgressAsync(string dailyWordleId, CancellationToken token);
    }
}
