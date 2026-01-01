using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    /// <summary>
    /// Repository interface cho VocabularyTopic (bảng trung gian)
    /// </summary>
    public interface IVocabularyTopicRepository
    {
        /// <summary>
        /// Lấy relationship theo vocabularyId và topicId
        /// </summary>
        Task<VocabularyTopic?> GetByVocabularyAndTopicAsync(string vocabularyId, string topicId);

        /// <summary>
        /// Lấy tất cả relationships của một vocabulary
        /// </summary>
        Task<List<VocabularyTopic>> GetByVocabularyIdAsync(string vocabularyId);

        /// <summary>
        /// Lấy tất cả relationships của một topic
        /// </summary>
        Task<List<VocabularyTopic>> GetByTopicIdAsync(string topicId);

        /// <summary>
        /// Thêm relationship mới
        /// </summary>
        Task AddAsync(VocabularyTopic vocabularyTopic);

        /// <summary>
        /// Cập nhật relationship
        /// </summary>
        Task UpdateAsync(VocabularyTopic vocabularyTopic);

        /// <summary>
        /// Xóa relationship (hard delete)
        /// </summary>
        Task DeleteAsync(VocabularyTopic vocabularyTopic);

        /// <summary>
        /// Lưu thay đổi vào database
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<(bool Success, int AddedCount, List<string> FailedItems)> AddVocabulariesToTopicWithTransactionAsync(
           string topicId,
           List<Vocabulary> vocabularies,
           string? currentUserId,
           CancellationToken cancellationToken = default);
        Task<(bool success, int removedCount, List<string> failedItems)>
      SoftRemoveVocabulariesFromTopicAsync(
          string topicId,
          List<string> vocabularyIds,
          string? removedBy,
          CancellationToken cancellationToken);
        Task<bool> HasActiveTopicAsync(string vocabularyId, CancellationToken cancellationToken);

        //Kho
        /// <summary>
        /// Này dùng lấy từ vựng để xuất excel theo topicId
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        Task<List<VocabularyExportDTO>> GetVocabsByTopicIdAsync(string topicId);
        //Kho
        /// <summary>
        /// Này lấy VocabId thui, dùng cho xem vocabId add vào topic bị trùng ko
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        Task<List<string>> GetVocabIdsByTopicIdAsync(string topicId);
    }
}
