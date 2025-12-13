using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class MeaningRepository : IMeaningRepository
    {
        private readonly TokkiDbContext _context;

        public MeaningRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Meaning?> GetByIdAsync(string meaningId)
        {
            return await _context.Meaning
                .Include(m => m.Word)
                .Include(m => m.MeaningTopics)
                    .ThenInclude(mt => mt.Topic)
                .FirstOrDefaultAsync(m => m.MeaningId == meaningId);
        }

        public async Task<List<Meaning>> GetByWordIdAsync(string wordId)
        {
            return await _context.Meaning
                .Include(m => m.MeaningTopics)
                    .ThenInclude(mt => mt.Topic)
                .Where(m => m.WordId == wordId && m.Status != MeaningStatus.Deleted)
                .ToListAsync();
        }

        public async Task<List<Meaning>> GetByIdsAsync(List<string> meaningIds)
        {
            return await _context.Meaning
                .Where(m => meaningIds.Contains(m.MeaningId))
                .Include(m => m.Word)
                .Include(m => m.MeaningTopics)
                .ToListAsync();
        }

        public async Task<(List<Meaning> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            MeaningStatus? status = null)
        {
            var query = _context.Meaning
                .Include(m => m.Word)
                .Include(m => m.MeaningTopics)
                    .ThenInclude(mt => mt.Topic)
                .AsQueryable();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(m =>
                    m.Definition.Contains(searchTerm) ||
                    (m.ExampleSentence != null && m.ExampleSentence.Contains(searchTerm)) ||
                    m.Word.Text.Contains(searchTerm)
                );
            }

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderByDescending(m => m.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Meaning meaning)
        {
            await _context.Meaning.AddAsync(meaning);
        }

        public async Task UpdateAsync(Meaning meaning)
        {
            _context.Meaning.Update(meaning);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Meaning meaning)
        {
            _context.Meaning.Remove(meaning);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Meaning?> GetMeaningByDefinitionAndTopicAsync(
            string wordId,
            string definition,
            string topicId)
        {
            return await _context.Meaning
                .Include(m => m.MeaningTopics)
                .Where(m => m.WordId == wordId
                    && m.Definition == definition
                    && m.MeaningTopics.Any(mt => mt.TopicId == topicId && mt.Status == MeaningTopicStatus.Active)
                    && m.Status == MeaningStatus.Active)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<Meaning> items, int totalCount)> GetPagedMeaningsByWordIdAsync(
            string wordId,
            int pageNumber,
            int pageSize,
            string? topicId = null,
            MeaningStatus? status = null)
        {
            var query = _context.Meaning
                .Include(m => m.MeaningTopics)
                .Where(m => m.WordId == wordId);

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            // Filter by topic
            if (!string.IsNullOrEmpty(topicId))
            {
                query = query.Where(m => m.MeaningTopics.Any(mt =>
                    mt.TopicId == topicId &&
                    mt.Status == MeaningTopicStatus.Active));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Meaning>> GetMeaningsByWordIdAndTopicIdAsync(string wordId, string topicId)
        {
            return await _context.Meaning
                .Include(m => m.MeaningTopics)
                .Where(m => m.WordId == wordId &&
                            m.MeaningTopics.Any(mt => mt.TopicId == topicId &&
                                                     mt.Status == MeaningTopicStatus.Active))
                .OrderByDescending(m => m.CreateDate)
                .ToListAsync();
        }

        // ========== METHODS MỚI ==========

        /// <summary>
        /// Xóa cứng Meaning theo ID (bao gồm cả MeaningTopics)
        /// </summary>
        public async Task<bool> DeleteByIdAsync(string meaningId)
        {
            var meaning = await _context.Meaning
                .Include(m => m.MeaningTopics)
                .FirstOrDefaultAsync(m => m.MeaningId == meaningId);

            if (meaning == null)
                return false;

            // Xóa các MeaningTopics liên quan trước
            if (meaning.MeaningTopics != null && meaning.MeaningTopics.Any())
            {
                _context.MeaningTopic.RemoveRange(meaning.MeaningTopics);
            }

            // Xóa Meaning
            _context.Meaning.Remove(meaning);

            return true;
        }

        /// <summary>
        /// Xóa mềm Meaning (đổi Status thành Deleted)
        /// </summary>
        public async Task<bool> SoftDeleteAsync(string meaningId, string userId)
        {
            var meaning = await _context.Meaning
                .FirstOrDefaultAsync(m => m.MeaningId == meaningId);

            if (meaning == null)
                return false;

            meaning.Status = MeaningStatus.Deleted;
            meaning.UpdateBy = userId;
            meaning.UpdateDate = DateTime.UtcNow;

            _context.Meaning.Update(meaning);

            return true;
        }

        /// <summary>
        /// Kiểm tra Meaning có tồn tại không
        /// </summary>
        public async Task<bool> ExistsAsync(string meaningId)
        {
            return await _context.Meaning
                .AnyAsync(m => m.MeaningId == meaningId);
        }
    }
}