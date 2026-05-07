using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    /// <summary>
    /// Repository interface cho Vocabulary entity
    /// </summary>
    public interface IVocabularyRepository
    {
        /// <summary>
        /// Lấy vocabulary theo ID
        /// </summary>
        Task<Vocabulary?> GetByIdAsync(string vocabularyId);

        /// <summary>
        /// Lấy vocabulary theo text và definition (để kiểm tra trùng lặp)
        /// </summary>
        Task<Vocabulary?> GetByTextAndDefinitionAsync(string text, string definition);

        /// <summary>
        /// Lấy tất cả vocabularies có cùng text (các nghĩa khác nhau)
        /// </summary>
        Task<List<Vocabulary>> GetByTextAsync(string text);

        /// <summary>
        /// Thêm vocabulary mới
        /// </summary>
        Task AddAsync(Vocabulary vocabulary);

        /// <summary>
        /// Cập nhật vocabulary
        /// </summary>
        Task UpdateAsync(Vocabulary vocabulary);

        /// <summary>
        /// Xóa vocabulary (hard delete)
        /// </summary>
        Task DeleteAsync(Vocabulary vocabulary);

        /// <summary>
        /// Lưu thay đổi vào database
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy vocabularies theo topic với phân trang
        /// </summary>
        Task<(List<Vocabulary> Items, int TotalCount)> GetPagedVocabulariesByTopicAsync(
            string topicId,
            int pageNumber,
            int pageSize,
            VocabularyStatus? status = null,
            string? searchText = null
        );

        /// <summary>
        /// Lấy vocabularies theo text với phân trang
        /// </summary>
        Task<(List<Vocabulary> Items, int TotalCount)> GetPagedVocabulariesByTextAsync(
            string text,
            int pageNumber,
            int pageSize,
            string? topicId = null,
            VocabularyStatus? status = null
        );
        Task<(List<VocabularySearchResultDto> Items, int TotalCount)>  SearchVocabulariesAsync(
         string searchTerm,
         int pageNumber,
         int pageSize);
        Task<List<Vocabulary>> GetByIdsAsync(List<string> vocabularyIds);

        Task<(IEnumerable<Vocabulary> Items, int TotalCount)> GetPagedVocabulariesForManagerAsync(
           int pageNumber,
           int pageSize,
           string? vocabId,
           VocabularyStatus? status,
           string? searchText,
           int? levelTopic);
        Task<Tokki.Domain.Entities.Vocabulary?> GetByIdWithChildrenAsync(string vocabularyId);


        //Hàm của Kho
        //Check xem có bị trùng text vs definition khi add = excel
        Task<List<Vocabulary>> GetExistingVocabEntitiesAsync(List<(string Text, string Definition)> inputs);
        //Hàm của Kho
        //Add nhiều vocab 1 lần
        Task AddRangeAsync(List<Vocabulary> vocabularies);


        //Hàm của kiệt
        Task<List<Vocabulary>> GetAllByTextAsync(string text);

        IExecutionStrategy CreateExecutionStrategy();
    }
}
