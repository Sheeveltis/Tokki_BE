using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserTopicProgressRepository
    {
        Task<UserTopicProgress?> GetByUserIdAndTopicIdAsync(string userId, string topicId);

        Task AddAsync(UserTopicProgress progress);
        void Update(UserTopicProgress progress);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<List<UserTopicProgress>> GetByUserIdAndTopicIdsAsync(string userId, List<string> topicIds);
    }
}
