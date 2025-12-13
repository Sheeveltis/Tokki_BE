using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IUserFavoriteTopicRepository
    {
        Task<UserFavoriteTopic?> GetByIdAsync(string favoriteTopicId);
        Task<UserFavoriteTopic?> GetByUserAndTopicAsync(string userId, string topicId);
        Task<(List<UserFavoriteTopic> Items, int TotalCount)> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            UserFavoriteTopicStatus? status = null);
        Task<UserFavoriteTopic> AddAsync(UserFavoriteTopic favoriteTopic);
        Task UpdateAsync(UserFavoriteTopic favoriteTopic);
        Task DeleteAsync(UserFavoriteTopic favoriteTopic);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
