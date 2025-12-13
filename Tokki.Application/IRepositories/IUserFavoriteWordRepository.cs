using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
     public interface IUserFavoriteWordRepository
    {
        Task<UserFavoriteWord?> GetByIdAsync(string favoriteWordId);
        Task<UserFavoriteWord?> GetByUserAndWordAsync(string userId, string wordId);
        Task<(List<UserFavoriteWord> Items, int TotalCount)> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            UserFavoriteWordStatus? status = null);
        Task<UserFavoriteWord> AddAsync(UserFavoriteWord favoriteWord);
        Task UpdateAsync(UserFavoriteWord favoriteWord);
        Task DeleteAsync(UserFavoriteWord favoriteWord);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }

}
