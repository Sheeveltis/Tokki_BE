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


        // Kiểm tra xem Category có tồn tại không (nhanh, nhẹ)
        Task<bool> CategoryExistsAsync(string categoryId);

        //  Đưa vào danh sách tên Tag (string) -> Trả về danh sách Entity Tag
        // (Nếu tag chưa có thì tự tạo mới, có rồi thì lấy ra dùng lại)
        Task<ICollection<Tag>> GetOrCreateTagsAsync(List<string> tagNames);

        // Transaction
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
