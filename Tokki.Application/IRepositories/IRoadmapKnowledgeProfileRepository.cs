using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IRoadmapKnowledgeProfileRepository
    {
        Task<RoadmapKnowledgeProfile?> GetAsync(
            string userRoadmapId,
            string questionTypeId,
            CancellationToken cancellationToken = default);

        Task<List<RoadmapKnowledgeProfile>> GetByRoadmapIdAsync(
            string userRoadmapId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            RoadmapKnowledgeProfile profile,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}