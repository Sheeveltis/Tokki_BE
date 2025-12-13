using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface ICommentRepository
    {
        Task AddAsync(Comment comment);
        Task UpdateAsync(Comment comment);

        Task<List<Comment>> GetByBlogIdAsync(string blogId, CancellationToken token);

        Task<Comment?> GetByIdAsync(string id);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}