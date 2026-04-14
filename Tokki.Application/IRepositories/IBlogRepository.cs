using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IBlogRepository
    {
        Task<PagedResult<Blog>> GetPagedAsync(int pageNumber, int pageSize, string? categoryId,
            string? tag, string? keyword, BlogStatus? status, bool? isOfficial, string? authorId = null, CancellationToken cancellationToken = default);
            
        Task<Blog?> GetByIdAsync(string id);
        Task AddAsync(Blog blog);
        Task UpdateAsync(Blog blog); 
        Task DeleteAsync(Blog blog); 
        Task<bool> CategoryExistsAsync(string categoryId);

        Task<ICollection<Tag>> GetOrCreateTagsAsync(List<string> tagNames, bool isVerified = false);
        Task DeleteTagAsync(Tag tag);

        Task<bool> ExistsAsync(string blogId);
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<bool> IncreaseViewCountAsync(string blogId);
        Task AddRangeAsync(IEnumerable<Blog> blogs, CancellationToken cancellationToken = default);
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Blog>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    }
}
