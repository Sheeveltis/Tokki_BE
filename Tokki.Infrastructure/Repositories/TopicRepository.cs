using Microsoft.CognitiveServices.Speech.Diagnostics.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
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
                .Include(t => t.VocabularyTopics)
                    .ThenInclude(vt => vt.Vocabulary)
                .FirstOrDefaultAsync(t => t.TopicId == topicId);
        }

        public async Task<Topic?> GetByNameAsync(string topicName)
        {
            return await _context.Topics
                .FirstOrDefaultAsync(t => t.TopicName == topicName);
        }

        public async Task<List<Topic>> GetByIdsAsync(List<string> topicIds)
        {
            return await _context.Topics
                .Where(t => topicIds.Contains(t.TopicId))
                .ToListAsync();
        }

        public async Task<(List<Topic> Items, int TotalCount)> GetPagedAsync(
      int pageNumber,
      int pageSize,
      string? searchTerm = null,
      TopicStatus? status = null,
      TopicLevel? level = null)
        {
            var query = _context.Topics
                .Include(t => t.VocabularyTopics)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t =>
                    t.TopicName.Contains(searchTerm) ||
                    (t.Description != null && t.Description.Contains(searchTerm))
                );
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }
            else
            {
                query = query.Where(t => t.Status != TopicStatus.Deleted);
            }

            if (level.HasValue)
            {
                query = query.Where(t => t.Level == level.Value);
            }

            int totalCount = await query.CountAsync();

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

        public async Task<int> CountVocabulariesInTopicAsync(string topicId)
        {
            return await _context.VocabularyTopics
                .Where(vt => vt.TopicId == topicId && vt.Status == VocabularyTopicStatus.Active)
                .Select(vt => vt.VocabularyId)
                .Distinct()
                .CountAsync();
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

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
