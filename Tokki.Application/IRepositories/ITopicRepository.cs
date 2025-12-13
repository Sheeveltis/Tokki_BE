using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface ITopicRepository
    {
        Task<Topic?> GetByIdAsync(string topicId);
        Task<Topic?> GetByNameAsync(string topicName);
        Task<List<Topic>> GetByIdsAsync(List<string> topicIds);
        Task<(List<Topic> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            TopicStatus? status = null);
        Task<bool> IsTopicNameExistsAsync(string topicName, string? excludeTopicId = null);
        Task AddAsync(Topic topic);
        Task UpdateAsync(Topic topic);
        Task DeleteAsync(Topic topic);
        Task<bool> HasMeaningsAsync(string topicId);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}