using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class TopicRepository : ITopicRepository
    {
        private readonly TokkiDbContext _context;

        public TopicRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Topic?> GetByIdAsync(string topicId)
        {
            return await _context.Topics
                .Include(t => t.Vocabularies)
                .FirstOrDefaultAsync(t => t.TopicId == topicId);
        }

        public async Task<(List<Topic> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            bool? isActive = null)
        {
            var query = _context.Topics
                .Include(t => t.Vocabularies)
                .AsQueryable();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t =>
                    t.TopicName.Contains(searchTerm) ||
                    (t.Description != null && t.Description.Contains(searchTerm))
                );
            }

            // Filter by active status
            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderByDescending(t => t.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> IsTopicNameExistsAsync(string topicName, string? excludeTopicId = null)
        {
            var query = _context.Topics.Where(t => t.TopicName == topicName);

            if (!string.IsNullOrEmpty(excludeTopicId))
            {
                query = query.Where(t => t.TopicId != excludeTopicId);
            }

            return await query.AnyAsync();
        }

        public async Task AddAsync(Topic topic)
        {
            await _context.Topics.AddAsync(topic);
        }

        public async Task UpdateAsync(Topic topic)
        {
            _context.Topics.Update(topic);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Topic topic)
        {
            _context.Topics.Remove(topic);
            await Task.CompletedTask;
        }

        public async Task<bool> HasVocabulariesAsync(string topicId)
        {
            return await _context.Vocabulary.AnyAsync(v => v.TopicId == topicId);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}