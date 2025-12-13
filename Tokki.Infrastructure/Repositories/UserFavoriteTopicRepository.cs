using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore; 

namespace Tokki.Infrastructure.Repositories
{
    public class UserFavoriteTopicRepository : IUserFavoriteTopicRepository
    {
        private readonly TokkiDbContext _context;

        public UserFavoriteTopicRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserFavoriteTopic?> GetByIdAsync(string favoriteTopicId)
        {
            return await _context.UserFavoriteTopics
                .Include(f => f.Topic)
                    .ThenInclude(t => t.MeaningTopics)
                .FirstOrDefaultAsync(f => f.FavoriteTopicId == favoriteTopicId);
        }

        public async Task<UserFavoriteTopic?> GetByUserAndTopicAsync(string userId, string topicId)
        {
            return await _context.UserFavoriteTopics
                .Include(f => f.Topic)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.TopicId == topicId);
        }

        public async Task<(List<UserFavoriteTopic> Items, int TotalCount)> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            UserFavoriteTopicStatus? status = null)
        {
            var query = _context.UserFavoriteTopics
                .Include(f => f.Topic)
                    .ThenInclude(t => t.MeaningTopics)
                .Where(f => f.UserId == userId);

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
            }

            // Search by topic name
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(f => f.Topic.TopicName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(f => f.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<UserFavoriteTopic> AddAsync(UserFavoriteTopic favoriteTopic)
        {
            await _context.UserFavoriteTopics.AddAsync(favoriteTopic);
            return favoriteTopic;
        }

        public Task UpdateAsync(UserFavoriteTopic favoriteTopic)
        {
            _context.UserFavoriteTopics.Update(favoriteTopic);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(UserFavoriteTopic favoriteTopic)
        {
            _context.UserFavoriteTopics.Remove(favoriteTopic);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
