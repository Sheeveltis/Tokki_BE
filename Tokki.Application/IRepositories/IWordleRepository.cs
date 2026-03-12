using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IWordleRepository
    {
        Task<DailyWordle?> GetDailyWordleWithVocabAsync(string dailyWordleId, CancellationToken token);

        Task AddSubmissionAsync(WordleSentenceSubmission submission, CancellationToken token);
        Task<WordleSentenceSubmission?> GetSubmissionByIdAsync(string submissionId, CancellationToken token);
        Task UpdateSubmissionAsync(WordleSentenceSubmission submission, CancellationToken token);
    }
}
