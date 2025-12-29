using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
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
                .Include(v => v.VocabularyExamples)
                .AsSplitQuery()
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
            string? searchText,
            TopicLevel? levelTopic)
        {
            var query = _context.Vocabularies.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(vocabId))
            {
                query = query.Where(v => v.VocabularyId == vocabId);
            }

            if (status.HasValue)
            {
                query = query.Where(v => v.Status == status.Value);
            }

            // Lọc theo Level của Topic
            if (levelTopic.HasValue)
            {
                var level = levelTopic.Value;

                query = query.Where(v =>
                    v.VocabularyTopics.Any(vt =>
                        vt.Topic.Level == level
                        && vt.Status == VocabularyTopicStatus.Active
                        && vt.Topic.Status == TopicStatus.Active
                    )
                );
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

        public async Task<(List<VocabularySearchResultDto> Items, int TotalCount)>
       SearchVocabulariesAsync(
           string searchTerm,
           int pageNumber,
           int pageSize)
        {
            var query = _context.Vocabularies
                .AsNoTracking()
                .Where(v => v.Status == VocabularyStatus.Active);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();

                // ✅ TÌM KIẾM HAI CHIỀU: Text ↔ Definition
                query = query.Where(v =>
                    v.Text.ToLower().Contains(term) ||           // Tìm trong Text (VD: 안녕)
                    v.Definition.ToLower().Contains(term) ||     // Tìm trong Definition (VD: Xin chào)
                    (v.Pronunciation != null &&
                     v.Pronunciation.ToLower().Contains(term))   // Tìm trong Pronunciation (VD: annyeong)
                );
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(v => v.Text.ToLower().StartsWith(searchTerm.ToLower()) ? 0 : 1) // Ưu tiên kết quả bắt đầu bằng search term
                .ThenBy(v => v.Text)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VocabularySearchResultDto
                {
                    VocabularyId = v.VocabularyId,
                    Text = v.Text,
                    Definition = v.Definition,
                    Pronunciation = v.Pronunciation
                })
                .ToListAsync();

            return (items, totalCount);
        }


        //Hàm của Kho
        //Check xem có bị trùng text vs definition khi add = excel
        public async Task<List<Vocabulary>> GetExistingVocabEntitiesAsync(List<(string Text, string Definition)> inputs)
        {
            var inputTexts = inputs.Select(x => x.Text).Distinct().ToList();

            var dbCandidates = await _context.Vocabularies
                .Where(v => inputTexts.Contains(v.Text) && v.Status == VocabularyStatus.Active)
                .ToListAsync();

            var matches = new List<Vocabulary>();

            foreach (var input in inputs)
            {
                var match = dbCandidates.FirstOrDefault(db =>
                    db.Text.Equals(input.Text, StringComparison.OrdinalIgnoreCase) &&
                    db.Definition.Equals(input.Definition, StringComparison.OrdinalIgnoreCase));

                if (match != null) matches.Add(match);
            }
            return matches; 
        }
        //Hàm của Kho
        //Add nhiều vocab 1 lần
        public async Task AddRangeAsync(List<Vocabulary> vocabularies)
        {
            await _context.Vocabularies.AddRangeAsync(vocabularies);
            await _context.SaveChangesAsync();
        }

        public async Task<Vocabulary?> GetByIdWithChildrenAsync(string vocabularyId)
        {
            return await _context.Vocabularies
                .Include(v => v.VocabularyTopics)
                .Include(v => v.VocabularyExamples)
                .FirstOrDefaultAsync(v => v.VocabularyId == vocabularyId);
        }

    }
}