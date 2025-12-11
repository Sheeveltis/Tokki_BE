using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface ITopicRepository
    {
        Task<Topic?> GetByIdAsync(string topicId);
        Task<(List<Topic> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, TopicStatus? status = null);
        Task<bool> IsTopicNameExistsAsync(string topicName, string? excludeTopicId = null);
        Task AddAsync(Topic topic);
        Task UpdateAsync(Topic topic);
        Task DeleteAsync(Topic topic);
        Task<bool> HasVocabulariesAsync(string topicId);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
