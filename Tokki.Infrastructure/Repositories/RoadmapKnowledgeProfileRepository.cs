using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class RoadmapKnowledgeProfileRepository : IRoadmapKnowledgeProfileRepository
    {
        private readonly TokkiDbContext _context;

        public RoadmapKnowledgeProfileRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<RoadmapKnowledgeProfile?> GetAsync(
            string userRoadmapId,
            string questionTypeId,
            CancellationToken cancellationToken = default)
        {
            return await _context.RoadmapKnowledgeProfiles
                .FirstOrDefaultAsync(
                    p => p.UserRoadmapId == userRoadmapId && p.QuestionTypeId == questionTypeId,
                    cancellationToken);
        }

        public async Task<List<RoadmapKnowledgeProfile>> GetByRoadmapIdAsync(
            string userRoadmapId,
            CancellationToken cancellationToken = default)
        {
            return await _context.RoadmapKnowledgeProfiles
                .Where(p => p.UserRoadmapId == userRoadmapId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            RoadmapKnowledgeProfile profile,
            CancellationToken cancellationToken = default)
        {
            await _context.RoadmapKnowledgeProfiles.AddAsync(profile, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}