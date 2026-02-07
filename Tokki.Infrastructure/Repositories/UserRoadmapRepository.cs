using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

public class UserRoadmapRepository : IUserRoadmapRepository
{
    private readonly TokkiDbContext _context;
    public UserRoadmapRepository(TokkiDbContext context) => _context = context;

    public async Task AddAsync(UserRoadmap roadmap)
    {
        await _context.UserRoadmaps.AddAsync(roadmap);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}