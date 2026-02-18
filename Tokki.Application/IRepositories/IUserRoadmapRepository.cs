using Tokki.Domain.Entities;

public interface IUserRoadmapRepository
{
    Task AddAsync(UserRoadmap roadmap);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}