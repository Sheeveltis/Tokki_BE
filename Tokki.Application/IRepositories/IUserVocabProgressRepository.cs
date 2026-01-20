using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserVocabProgressRepository
    {
        Task<UserVocabProgress?> GetByVocabIdAsync(string userId, string vocabularyId, CancellationToken cancellationToken);

        Task AddAsync(UserVocabProgress progress, CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<List<ReviewItemDTO>> GetDueReviewsAsync(string userId, DateTime compareTime, int limit, CancellationToken cancellationToken = default);
        Task<List<string>> GetLearnedVocabIdsByTopicAsync(string userId, string topicId);
    }
}
