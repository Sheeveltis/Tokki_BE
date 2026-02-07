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

        Task<PagedResult<Blog>> GetPagedAsync(int pageNumber,int pageSize,string? categoryId,
        BlogStatus? status, CancellationToken cancellationToken);
        Task<Blog?> GetByIdAsync(string id);
        Task AddAsync(Blog blog);
        Task UpdateAsync(Blog blog); 
        Task DeleteAsync(Blog blog); 
        Task<bool> CategoryExistsAsync(string categoryId);

        Task<ICollection<Tag>> GetOrCreateTagsAsync(List<string> tagNames);

        Task<bool> ExistsAsync(string blogId);
        // Transaction
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<bool> IncreaseViewCountAsync(string blogId);
    }
}
