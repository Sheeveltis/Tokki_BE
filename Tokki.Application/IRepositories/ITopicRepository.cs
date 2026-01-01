using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.DTOs;
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
        TopicStatus? status = null,
        TopicLevel? level = null);
        Task<(List<Topic> Items, int TotalCount)> GetPagedForUserAsync(
       int pageNumber,
       int pageSize,
       string? searchTerm = null,
       TopicLevel? level = null);

        Task<bool> IsTopicNameExistsAsync(string topicName, string? excludeTopicId = null);
        Task<int> CountVocabulariesInTopicAsync(string topicId);
        Task AddAsync(Topic topic);
        Task UpdateAsync(Topic topic);
        Task DeleteAsync(Topic topic);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        //Kho
        /// <summary>
        /// Này lấy tên topic từ topicId (dùng đặt tên file xuất excel)
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        Task<string?> GetTopicNameAsync(string topicId);
    }
}
