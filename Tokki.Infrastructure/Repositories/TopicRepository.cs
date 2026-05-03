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
      int? level = null)
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
            topicName = topicName.Trim();

            var query = _context.Topics
                .Where(t => t.TopicName == topicName && t.Status != TopicStatus.Deleted);

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
        public async Task<(List<Topic> items, int totalCount)> GetVocabTopicsPagedForUserAsync(
            int pageNumber, int pageSize, string? searchTerm = null, int? level = null)
        {
            var query = _context.Topics
                .Include(t => t.VocabularyTopics)
                .Where(t => t.Status == TopicStatus.Active
                         && t.TopicType == TopicType.VocabStudy) // ✅ chỉ lấy VocabStudy
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(t =>
                    t.TopicName.Contains(searchTerm) ||
                    (t.Description != null && t.Description.Contains(searchTerm))
                );

            if (level.HasValue)
                query = query.Where(t => t.Level == level.Value);

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(t => t.OrderIndex.HasValue ? 0 : 1) // ✅ có index lên trước
                .ThenBy(t => t.OrderIndex)                    // ✅ sắp xếp theo index tăng dần
                .ThenByDescending(t => t.CreateDate)          // ✅ không có index thì theo ngày tạo
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<(List<Topic> Items, int TotalCount)> GetPagedForUserAsync(int pageNumber, int pageSize, string? searchTerm = null, int? level = null)
        {
            var query = _context.Topics
                .Include(t => t.VocabularyTopics)
                .Where(t => t.Status == TopicStatus.Active) // Chỉ lấy Status = 1 (Active)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t =>
                    t.TopicName.Contains(searchTerm) ||
                    (t.Description != null && t.Description.Contains(searchTerm))
                );
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
        //Kho
        /// <summary>
        /// Này lấy tên topic từ topicId (dùng đặt tên file xuất excel)
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public async Task<string?> GetTopicNameAsync(string topicId)
        {
            // Lấy tên Topic để làm tên File
            return await _context.Topics
                .Where(t => t.TopicId == topicId)
                .Select(t => t.TopicName)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Kho - Này dùng cho việc tính toán tiến độ học từ vựng của topic đó tới đâu rồi
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public async Task<int> CountLearnedVocabulariesAsync(string userId, string topicId)
        {
            var vocabIdsInTopic = _context.VocabularyTopics
                .AsNoTracking()
                .Where(vt => vt.TopicId == topicId)
                .Select(vt => vt.VocabularyId);
            return await _context.UserVocabProgresses
                .AsNoTracking()
                .CountAsync(uv => uv.UserId == userId
                                  && vocabIdsInTopic.Contains(uv.VocabularyId));
        }
        /// <summary>
        /// Kho - Lấy tất cả từ vựng trong topic để tính toán lấy từ vựng phù hợp cho người dùng học
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public async Task<List<Vocabulary>> GetVocabulariesByTopicIdAsync(string topicId)
        {
            var query = from vt in _context.VocabularyTopics
                        join v in _context.Vocabularies on vt.VocabularyId equals v.VocabularyId
                        where vt.TopicId == topicId
                        && v.Status == VocabularyStatus.Active 
                        select v;

            return await query.AsNoTracking().ToListAsync();
        }

        //Hàm  của kiệt - Lấy max OrderIndex hiện tại để khi tạo topic mới sẽ gán OrderIndex = max + 1

        public async Task<int> GetMaxOrderIndexAsync()
        {
            return await _context.Topics
                .Where(t => t.Status != TopicStatus.Deleted
                            && t.TopicType == TopicType.VocabStudy)
                .Select(t => (int?)t.OrderIndex)
                .MaxAsync() ?? 0;
        }
        public async Task<int> GetMaxOrderIndexForVocabAsync()
        {
            return await _context.Topics
                .Where(t => t.Status != TopicStatus.Deleted
                            && t.TopicType == TopicType.VocabStudy)
                .Select(t => (int?)t.OrderIndex)
                .MaxAsync() ?? 0;
        }
        public async Task DecrementOrderIndexAfterAsync(
    int deletedOrderIndex,
    TopicType topicType,
    string updatedBy,
    DateTime updatedDate)
        {
            // chỉ lùi index các topic phía sau, cùng type, và không phải Deleted
            var topicsToShift = await _context.Topics
                .Where(t => t.Status != TopicStatus.Deleted
                            && t.TopicType == topicType
                            && t.OrderIndex > deletedOrderIndex)
                .ToListAsync();

            foreach (var t in topicsToShift)
            {
                t.OrderIndex -= 1;
                t.UpdateBy = updatedBy;
                t.UpdateDate = updatedDate;
            }
        }

        public async Task ShiftOrderIndexUpFromAsync(
    int fromIndex,
    TopicType topicType,
    string excludeTopicId,
    string updatedBy,
    DateTime updatedDate)
        {
            var topicsToShift = await _context.Topics
                .Where(t => t.Status != TopicStatus.Deleted
                            && t.TopicType == topicType
                            && t.OrderIndex >= fromIndex
                            && t.TopicId != excludeTopicId)
                .OrderByDescending(t => t.OrderIndex) // tránh dính unique (nếu có)
                .ToListAsync();

            foreach (var t in topicsToShift)
            {
                t.OrderIndex += 1;
                t.UpdateBy = updatedBy;
                t.UpdateDate = updatedDate;
            }
        }

        // TopicRepository.cs
        public async Task<(IEnumerable<Topic> items, int totalCount)> GetVocabTopicsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            TopicStatus? status,
            int? level)
        {
            var query = _context.Topics
                .Where(t => t.TopicType == TopicType.VocabStudy) // ✅ filter VocabStudy từ DB
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(t => t.TopicName.Contains(searchTerm));

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (level.HasValue)
                query = query.Where(t => t.Level == level.Value);

            var totalCount = await query.CountAsync();

            var items = await query
              .OrderBy(t => t.OrderIndex.HasValue ? 0 : 1)
              .ThenBy(t => t.OrderIndex)                   
              .ThenBy(t => t.CreateDate)                   
              .Skip((pageNumber - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync();

            return (items, totalCount);
        }


        public async Task ShiftOrderIndexBetweenAsync(
    int fromIndex, int toIndex, TopicType topicType,
    string excludeTopicId, string updatedBy, DateTime updatedDate)
        {
            bool movingDown = fromIndex < toIndex;

            var topicsToShift = await _context.Topics
                .Where(t => t.TopicType == topicType
                         && t.TopicId != excludeTopicId
                         && t.OrderIndex.HasValue
                         && (movingDown
                             ? t.OrderIndex >= fromIndex + 1 && t.OrderIndex <= toIndex   // dịch xuống: giảm 1
                             : t.OrderIndex >= toIndex && t.OrderIndex <= fromIndex - 1)) // dịch lên: tăng 1
                .ToListAsync();

            foreach (var t in topicsToShift)
            {
                t.OrderIndex = movingDown ? t.OrderIndex - 1 : t.OrderIndex + 1;
                t.UpdateBy = updatedBy;
                t.UpdateDate = updatedDate;
            }
        }
    }
}
