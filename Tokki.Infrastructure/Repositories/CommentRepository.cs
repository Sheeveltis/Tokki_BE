using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
public class CommentRepository : ICommentRepository
{
    private readonly TokkiDbContext _context;

    public CommentRepository(TokkiDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
    }

    public async Task UpdateAsync(Comment comment)
    {
        _context.Comments.Update(comment);
    }

    public async Task<List<Comment>> GetByBlogIdAsync(string blogId, CancellationToken token)
    {
        return await _context.Comments
            .AsNoTracking() 
            .Where(c => c.BlogId == blogId)
            .Include(c => c.User)
                .ThenInclude(u => u.CurrentTitle)
            .OrderBy(c => c.CreatedAt) 
            .ToListAsync(token);
    }

    public async Task<Comment?> GetByIdAsync(string id)
    {
        return await _context.Comments.FindAsync(id);
    }
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}