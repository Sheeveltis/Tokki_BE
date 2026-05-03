using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface ITopicRepository
    {
        Task<Topic?> GetByIdAsync(string topicId);
        Task<Topic?> GetByNameAsync(string topicName);
        Task<List<Topic>> GetByIdsAsync(List<string> topicIds);
        Task<(List<Topic> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TopicStatus? status = null,
        int? level = null);
        Task<(List<Topic> Items, int TotalCount)> GetPagedForUserAsync(
       int pageNumber,
       int pageSize,
       string? searchTerm = null,
       int? level = null);
        Task<(List<Topic> items, int totalCount)> GetVocabTopicsPagedForUserAsync(
    int pageNumber,
    int pageSize,
    string? searchTerm = null,
    int? level = null
);
        Task<bool> IsTopicNameExistsAsync(string topicName, string? excludeTopicId = null);
        Task<int> CountVocabulariesInTopicAsync(string topicId);
        Task AddAsync(Topic topic);
        Task UpdateAsync(Topic topic);
        Task DeleteAsync(Topic topic);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        //Kho
        /// <summary>
        /// Này lấy tên topic từ topicId (dùng đặt tên file xuất excel)
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        Task<string?> GetTopicNameAsync(string topicId);

        /// <summary>
        /// Kho - Này dùng cho việc tính toán tiến độ học từ vựng của topic đó tới đâu rồi
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="topicId"></param>
        /// <returns></returns>
        Task<int> CountLearnedVocabulariesAsync(string userId, string topicId);
        /// <summary>
        /// Kho - Lấy tất cả từ vựng trong topic để tính toán lấy từ vựng phù hợp cho người dùng học
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        Task<List<Vocabulary>> GetVocabulariesByTopicIdAsync(string topicId);

        //Hàm  của kiệt - Lấy max OrderIndex hiện tại để khi tạo topic mới sẽ gán OrderIndex = max + 1
        Task<int> GetMaxOrderIndexAsync();
        //Hàm  của kiệt - Lấy max OrderIndex hiện tại để khi tạo topic mới sẽ gán OrderIndex = max + 1

        Task<int> GetMaxOrderIndexForVocabAsync();
        //Hàm  của kiệt - Khi xóa topic thì những topic có OrderIndex lớn hơn topic bị xóa sẽ bị giảm OrderIndex đi 1 để tránh bị "lỗ" trong thứ tự hiển thị
        Task DecrementOrderIndexAfterAsync(
        int deletedOrderIndex,
        TopicType topicType,
        string updatedBy,
        DateTime updatedDate);
        //Hàm  của kiệt -  dùng để chèn topic mới vào giữa 2 topic đã có thứ tự (ví dụ: muốn chèn vào vị trí index 2 thì những topic hiện tại có index >= 2 sẽ bị tăng index lên 1)
        Task ShiftOrderIndexUpFromAsync(
    int fromIndex,
    TopicType topicType,
    string excludeTopicId,
    string updatedBy,
    DateTime updatedDate);

        //Hàm của kiệt - get topic cho vocab 
        Task<(IEnumerable<Topic> items, int totalCount)> GetVocabTopicsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            TopicStatus? status,
            int? level
        );
        //Hàm của kiệt
        Task ShiftOrderIndexBetweenAsync(
    int fromIndex,
    int toIndex,
    TopicType topicType,
    string excludeTopicId,
    string updatedBy,
    DateTime updatedDate);
    }
}
