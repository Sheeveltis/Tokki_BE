using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class VocabularyTopicRepository : IVocabularyTopicRepository
    {
        private readonly TokkiDbContext _context;

        public VocabularyTopicRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<VocabularyTopic?> GetByVocabularyAndTopicAsync(string vocabularyId, string topicId)
        {
            return await _context.VocabularyTopics
                .Include(vt => vt.Vocabulary)
                .Include(vt => vt.Topic)
                .FirstOrDefaultAsync(vt =>
                    vt.VocabularyId == vocabularyId &&
                    vt.TopicId == topicId);
        }

        public async Task<List<VocabularyTopic>> GetByVocabularyIdAsync(string vocabularyId)
        {
            return await _context.VocabularyTopics
                .Include(vt => vt.Topic)
                .Where(vt => vt.VocabularyId == vocabularyId)
                .ToListAsync();
        }

        public async Task<List<VocabularyTopic>> GetByTopicIdAsync(string topicId)
        {
            return await _context.VocabularyTopics
                .Include(vt => vt.Vocabulary)
                .Where(vt => vt.TopicId == topicId)
                .ToListAsync();
        }

        public async Task AddAsync(VocabularyTopic vocabularyTopic)
        {
            await _context.VocabularyTopics.AddAsync(vocabularyTopic);
        }

        public async Task UpdateAsync(VocabularyTopic vocabularyTopic)
        {
            _context.VocabularyTopics.Update(vocabularyTopic);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(VocabularyTopic vocabularyTopic)
        {
            _context.VocabularyTopics.Remove(vocabularyTopic);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // ============================================
        // Method mới: Add nhiều vocabularies với Transaction
        // ============================================
        public async Task<(bool Success, int AddedCount, List<string> FailedItems)> AddVocabulariesToTopicWithTransactionAsync(
            string topicId,
            List<Vocabulary> vocabularies,
            string? currentUserId,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var failedItems = new List<string>();
            var addedCount = 0;

            try
            {
                foreach (var vocab in vocabularies)
                {
                    // Kiểm tra trạng thái của vocabulary
                    if (vocab.Status == VocabularyStatus.Deleted)
                    {
                        failedItems.Add($"{vocab.Text} (ID: {vocab.VocabularyId}) - Từ vựng đã bị xóa");
                        throw new Exception($"Vocabulary '{vocab.Text}' is deleted");
                    }

                    if (vocab.Status == VocabularyStatus.Inactive)
                    {
                        failedItems.Add($"{vocab.Text} (ID: {vocab.VocabularyId}) - Từ vựng đang không hoạt động");
                        throw new Exception($"Vocabulary '{vocab.Text}' is inactive");
                    }

                    // Kiểm tra xem liên kết này đã tồn tại chưa
                    var existingLink = await _context.VocabularyTopics
                        .FirstOrDefaultAsync(vt =>
                            vt.VocabularyId == vocab.VocabularyId &&
                            vt.TopicId == topicId,
                            cancellationToken);

                    if (existingLink == null)
                    {
                        // Tạo mới liên kết
                        var newLink = new VocabularyTopic
                        {
                            TopicId = topicId,
                            VocabularyId = vocab.VocabularyId,
                            Status = VocabularyTopicStatus.Active,
                            CreateDate = DateTime.UtcNow.AddHours(7),
                            CreateBy = currentUserId
                        };

                        await _context.VocabularyTopics.AddAsync(newLink, cancellationToken);
                        addedCount++;
                    }
                    else
                    {
                        // Nếu đã tồn tại và Active thì bỏ qua
                        if (existingLink.Status == VocabularyTopicStatus.Active)
                        {
                            continue;
                        }

                        // Nếu đã tồn tại nhưng bị Deleted/Inactive, kích hoạt lại
                        existingLink.Status = VocabularyTopicStatus.Active;
                       

                        _context.VocabularyTopics.Update(existingLink);
                        addedCount++;
                    }
                }

                // Nếu có từ vựng nào thất bại, rollback
                if (failedItems.Any())
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, 0, failedItems);
                }

                // Save changes và commit transaction
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return (true, addedCount, new List<string>());
            }
            catch (Exception ex)
            {
                // Rollback nếu có lỗi
                await transaction.RollbackAsync(cancellationToken);

                // Nếu chưa có thông tin lỗi chi tiết, thêm vào
                if (!failedItems.Any())
                {
                    failedItems.Add($"Lỗi hệ thống: {ex.Message}");
                }

                return (false, 0, failedItems);
            }
        }
        public async Task<(bool success, int removedCount, List<string> failedItems)>
            SoftRemoveVocabulariesFromTopicAsync(
                string topicId,
                List<string> vocabularyIds,
                string? removedBy,
                CancellationToken cancellationToken)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var failedItems = new List<string>();

            try
            {
                // 1. Lấy các mapping còn ACTIVE
                var vocabTopicRecords = await _context.VocabularyTopics
                    .AsTracking()
                    .Where(vt =>
                        vt.TopicId == topicId &&
                        vocabularyIds.Contains(vt.VocabularyId) &&
                        vt.Status == VocabularyTopicStatus.Active)
                    .ToListAsync(cancellationToken);

                if (!vocabTopicRecords.Any())
                {
                    return (true, 0, failedItems);
                }

                // 2. Soft delete
                foreach (var item in vocabTopicRecords)
                {
                    item.Status = VocabularyTopicStatus.Deleted;
                    item.UpdateBy = removedBy;
                    item.UpdateDate = DateTime.UtcNow.AddHours(7);
                }

                _context.VocabularyTopics.UpdateRange(vocabTopicRecords);

                var removedCount = await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return (true, removedCount, failedItems);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                failedItems.Add(ex.Message);
                return (false, 0, failedItems);
            }
        }
        public async Task<bool> HasActiveTopicAsync(
    string vocabularyId,
    CancellationToken cancellationToken)
        {
            return await _context.VocabularyTopics
                .AnyAsync(vt =>
                    vt.VocabularyId == vocabularyId &&
                    vt.Status == VocabularyTopicStatus.Active,
                    cancellationToken);
        }

    }
}