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
    public class WordRepository : IWordRepository
    {
        private readonly TokkiDbContext _context;

        public WordRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Word?> GetByIdAsync(string wordId)
        {
            return await _context.Word
                .Include(w => w.Meanings)
                    .ThenInclude(m => m.MeaningTopics)
                        .ThenInclude(mt => mt.Topic)
                .FirstOrDefaultAsync(w => w.WordId == wordId);
        }

        public async Task<Word?> GetByTextAsync(string text)
        {
            return await _context.Word
                .Include(w => w.Meanings)
                .FirstOrDefaultAsync(w => w.Text == text);
        }

        public async Task<List<Word>> GetByIdsAsync(List<string> wordIds)
        {
            return await _context.Word
                .Where(w => wordIds.Contains(w.WordId))
                .Include(w => w.Meanings)
                .ToListAsync();
        }

        public async Task<(List<Word> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            WordStatus? status = null)
        {
            var query = _context.Word
                .Include(w => w.Meanings)
                .AsQueryable();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(w =>
                    w.Text.Contains(searchTerm) ||
                    (w.Pronunciation != null && w.Pronunciation.Contains(searchTerm))
                );
            }

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(w => w.Status == status.Value);
            }

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderByDescending(w => w.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> IsWordTextExistsAsync(string text, string? excludeWordId = null)
        {
            var query = _context.Word.Where(w => w.Text == text);

            if (!string.IsNullOrEmpty(excludeWordId))
            {
                query = query.Where(w => w.WordId != excludeWordId);
            }

            return await query.AnyAsync();
        }

        public async Task AddAsync(Word word)
        {
            await _context.Word.AddAsync(word);
        }

        public async Task UpdateAsync(Word word)
        {
            _context.Word.Update(word);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Word word)
        {
            _context.Word.Remove(word);
            await Task.CompletedTask;
        }
        public async Task<(List<Word> items, int totalCount)> GetPagedWordsByTopicIdAsync(
    string topicId,
    int pageNumber,
    int pageSize,
    string? searchTerm = null,
    WordStatus? status = null)
        {
            // Lấy tất cả wordIds có meanings trong topic này
            var wordIdsInTopic = await _context.MeaningTopic
                .Where(mt => mt.TopicId == topicId && mt.Status == MeaningTopicStatus.Active)
                .Join(_context.Meaning,
                    mt => mt.MeaningId,
                    m => m.MeaningId,
                    (mt, m) => m.WordId)
                .Distinct()
                .ToListAsync();

            var query = _context.Word
                .Where(w => wordIdsInTopic.Contains(w.WordId));

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(w => w.Status == status.Value);
            }

            // Search by text
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(w => w.Text.Contains(searchTerm) ||
                                         (w.Pronunciation != null && w.Pronunciation.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(w => w.Text)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    
    }
}
