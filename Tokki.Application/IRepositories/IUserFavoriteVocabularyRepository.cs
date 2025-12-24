using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserFavoriteVocabularyRepository
    {
        Task<bool> ExistsAsync(string userId, string vocabularyId, CancellationToken cancellationToken);
        Task AddAsync(UserFavoriteVocabulary entity, CancellationToken cancellationToken);
        Task<int> HardDeleteAsync(string userId, string vocabularyId, CancellationToken cancellationToken);
        Task<(List<UserFavoriteVocabulary> items, int totalCount)> GetPagedByUserAndTopicAsync(
          string userId,
          string? topicId,
          int pageNumber,
          int pageSize,
          string? searchTerm,
          CancellationToken cancellationToken);
    }
}
