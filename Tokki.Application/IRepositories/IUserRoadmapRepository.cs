using Tokki.Domain.Entities;

public interface IUserRoadmapRepository
{
    Task AddAsync(UserRoadmap roadmap);
    Task <bool>SaveChangesAsync(CancellationToken cancellationToken);
    Task<UserRoadmap?> GetActiveRoadmapByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<RoadmapDailyTask?> GetTaskByIdAsync(string taskId, CancellationToken cancellationToken = default);
}