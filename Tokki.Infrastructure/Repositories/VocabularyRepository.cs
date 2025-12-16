using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class VocabularyRepository : IVocabularyRepository
    {
        private readonly TokkiDbContext _context;

        public VocabularyRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Vocabulary?> GetByIdAsync(string vocabularyId)
        {
            return await _context.Vocabularies
                .Include(v => v.VocabularyTopics)
                    .ThenInclude(vt => vt.Topic)
                .FirstOrDefaultAsync(v => v.VocabularyId == vocabularyId);
        }

        public async Task<Vocabulary?> GetByTextAndDefinitionAsync(string text, string definition)
        {
            return await _context.Vocabularies
                .FirstOrDefaultAsync(v => v.Text == text && v.Definition == definition && v.Status != VocabularyStatus.Deleted);
        }

        public async Task<List<Vocabulary>> GetByTextAsync(string text)
        {
            return await _context.Vocabularies
                .Where(v => v.Text == text && v.Status != VocabularyStatus.Deleted)
                .Include(v => v.VocabularyTopics)
                    .ThenInclude(vt => vt.Topic)
                .ToListAsync();
        }

        public async Task AddAsync(Vocabulary vocabulary)
        {
            await _context.Vocabularies.AddAsync(vocabulary);
        }

        public async Task UpdateAsync(Vocabulary vocabulary)
        {
            _context.Vocabularies.Update(vocabulary);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Vocabulary vocabulary)
        {
            _context.Vocabularies.Remove(vocabulary);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<(List<Vocabulary> Items, int TotalCount)> GetPagedVocabulariesByTopicAsync(
            string topicId,
            int pageNumber,
            int pageSize,
            VocabularyStatus? status = null,
            string? searchText = null)
        {
            var query = _context.Vocabularies
                .Include(v => v.VocabularyTopics)
                    .ThenInclude(vt => vt.Topic)
                .Where(v => v.VocabularyTopics.Any(vt => 
                    vt.TopicId == topicId && 
                    vt.Status == VocabularyTopicStatus.Active));

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(v => v.Status == status.Value);
            }
            else
            {
                // Mặc định không lấy Deleted
                query = query.Where(v => v.Status != VocabularyStatus.Deleted);
            }

            // Search by text or definition
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(v => 
                    v.Text.Contains(searchText) || 
                    v.Definition.Contains(searchText));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(v => v.Text)
                .ThenBy(v => v.Definition)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Vocabulary> Items, int TotalCount)> GetPagedVocabulariesByTextAsync(
            string text,
            int pageNumber,
            int pageSize,
            string? topicId = null,
            VocabularyStatus? status = null)
        {
            var query = _context.Vocabularies
                .Include(v => v.VocabularyTopics)
                    .ThenInclude(vt => vt.Topic)
                .Where(v => v.Text == text);

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(v => v.Status == status.Value);
            }
            else
            {
                // Mặc định không lấy Deleted
                query = query.Where(v => v.Status != VocabularyStatus.Deleted);
            }

            // Filter by topic if provided
            if (!string.IsNullOrWhiteSpace(topicId))
            {
                query = query.Where(v => v.VocabularyTopics.Any(vt => 
                    vt.TopicId == topicId && 
                    vt.Status == VocabularyTopicStatus.Active));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(v => v.Definition)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<List<Vocabulary>> GetByIdsAsync(List<string> vocabularyIds)
        {
            return await _context.Vocabularies
                .Where(v => vocabularyIds.Contains(v.VocabularyId))
                .ToListAsync();
        }
        // Tokki.Infrastructure.Repositories.VocabularyRepository.cs
        public async Task<(IEnumerable<Vocabulary> Items, int TotalCount)> GetPagedVocabulariesForManagerAsync(
            int pageNumber,
            int pageSize,
            string? vocabId,
            VocabularyStatus? status,
            string? searchText)
        {
            var query = _context.Vocabularies.AsNoTracking().AsQueryable();

            // Lọc theo VocabId (thay cho TopicId)
            if (!string.IsNullOrWhiteSpace(vocabId))
            {
                query = query.Where(v => v.VocabularyId == vocabId);
            }

            if (status.HasValue)
            {
                query = query.Where(v => v.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var text = searchText.ToLower().Trim();
                query = query.Where(v =>
                    v.Text.ToLower().Contains(text) ||
                    v.Definition.ToLower().Contains(text) ||
                    (v.Pronunciation != null && v.Pronunciation.ToLower().Contains(text))
                );
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(v => v.CreateDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

    }
}
