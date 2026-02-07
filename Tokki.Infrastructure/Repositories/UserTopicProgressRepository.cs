using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tokki.Infrastructure.Repositories
{
    public class UserTopicProgressRepository : IUserTopicProgressRepository
    {
        private readonly TokkiDbContext _context;

        public UserTopicProgressRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserTopicProgress?> GetByUserIdAndTopicIdAsync(string userId, string topicId)
        {
            return await _context.UserTopicProgresses
                .FirstOrDefaultAsync(x => x.UserId == userId && x.TopicId == topicId);
        }

        public async Task AddAsync(UserTopicProgress progress)
        {
            await _context.UserTopicProgresses.AddAsync(progress);
        }

        public void Update(UserTopicProgress progress)
        {
            _context.UserTopicProgresses.Update(progress);
        }
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<List<UserTopicProgress>> GetByUserIdAndTopicIdsAsync(string userId, List<string> topicIds)
        {
            return await _context.UserTopicProgresses
                .AsNoTracking()
                .Where(x => x.UserId == userId && topicIds.Contains(x.TopicId))
                .ToListAsync();
        }
    }
}
