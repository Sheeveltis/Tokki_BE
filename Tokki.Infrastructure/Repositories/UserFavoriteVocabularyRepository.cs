using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserFavoriteVocabularyRepository : IUserFavoriteVocabularyRepository
    {
        private readonly TokkiDbContext _context;

        public UserFavoriteVocabularyRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(string userId, string vocabularyId, CancellationToken cancellationToken)
        {
            return await _context.UserFavoriteVocabularies
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.VocabularyId == vocabularyId, cancellationToken);
        }

        public async Task AddAsync(UserFavoriteVocabulary entity, CancellationToken cancellationToken)
        {
            await _context.UserFavoriteVocabularies.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

    
        public async Task<int> HardDeleteAsync(string userId, string vocabularyId, CancellationToken cancellationToken)
        {
            var favorite = await _context.UserFavoriteVocabularies
                .FirstOrDefaultAsync(x => x.UserId == userId && x.VocabularyId == vocabularyId, cancellationToken);

            if (favorite == null)
                return 0;

            _context.UserFavoriteVocabularies.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);

            return 1;
        }
        public async Task<(List<UserFavoriteVocabulary> items, int totalCount)> GetPagedByUserAndTopicAsync(
         string userId,
         string? topicId,
         int pageNumber,
         int pageSize,
         string? searchTerm,
         CancellationToken cancellationToken)
        {
            // normalize inputs
            var normalizedTopicId = string.IsNullOrWhiteSpace(topicId) ? null : topicId.Trim();
            var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

            // base query: favorites of user + vocab must be Active
            IQueryable<UserFavoriteVocabulary> query = _context.UserFavoriteVocabularies
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Include(f => f.Vocabulary) // để handler map DTO dùng f.Vocabulary.*
                .Where(f => f.Vocabulary.Status == VocabularyStatus.Active);

            // optional search
            if (normalizedSearch != null)
            {
                query = query.Where(f =>
                    f.Vocabulary.Text.Contains(normalizedSearch) ||
                    f.Vocabulary.Definition.Contains(normalizedSearch));
            }

            // optional topic filter
            if (normalizedTopicId != null)
            {
                query = query.Where(f =>
                    f.Vocabulary.VocabularyTopics.Any(vt =>
                        vt.TopicId == normalizedTopicId &&
                        vt.Status == VocabularyTopicStatus.Active &&
                        vt.Topic.Status == TopicStatus.Active));
            }

            // total count (before paging)
            var totalCount = await query.CountAsync(cancellationToken);

            // paging + order newest favorite first
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .ThenByDescending(f => f.FavoriteVocabularyId) // ổn định thứ tự nếu CreatedAt trùng
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
