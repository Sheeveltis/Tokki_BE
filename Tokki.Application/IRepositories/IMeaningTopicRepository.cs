using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IMeaningTopicRepository
    {
        Task<MeaningTopic?> GetByIdAsync(string meaningId, string topicId);
        Task<List<MeaningTopic>> GetByMeaningIdAsync(string meaningId);
        Task<List<MeaningTopic>> GetByTopicIdAsync(string topicId);
        Task<List<MeaningTopic>> GetByMeaningIdsAsync(List<string> meaningIds);
        Task<bool> ExistsAsync(string meaningId, string topicId);
        Task AddAsync(MeaningTopic meaningTopic);
        Task DeleteAsync(MeaningTopic meaningTopic);
        Task DeleteRangeByMeaningIdAsync(string meaningId);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<List<Meaning>> GetMeaningsByWordIdAndTopicIdAsync(string wordId, string topicId);

    }
}
