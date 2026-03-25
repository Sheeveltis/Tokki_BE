using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IQuestionBankRepository
    {
        Task<QuestionBank?> GetByIdAsync(string questionBankId, CancellationToken cancellationToken = default);

        Task<QuestionBank?> GetByIdWithDetailsAsync(string questionBankId, CancellationToken cancellationToken = default);

        Task<(IEnumerable<QuestionBank> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? questionTypeId = null,
            string? passageId = null,
            QuestionBankStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<QuestionBank>> GetByPassageIdAsync(string passageId, CancellationToken cancellationToken = default);

        Task<IEnumerable<QuestionBank>> GetByQuestionTypeIdAsync(string questionTypeId, CancellationToken cancellationToken = default);

        Task AddAsync(QuestionBank questionBank);

        Task UpdateAsync(QuestionBank questionBank);

        Task DeleteAsync(QuestionBank questionBank);

        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<List<QuestionBank>> GetByIdsAsync(IEnumerable<string> questionBankIds, CancellationToken cancellationToken = default);

        Task UpdateRangeAsync(IEnumerable<QuestionBank> questionBanks);
        Task<bool> AnyUsingPassageAsync(string passageId, CancellationToken cancellationToken = default);
        Task<IEnumerable<QuestionBank>> GetByQuestionTypeIdAsync(
            string questionTypeId,
            QuestionBankStatus? status,
            CancellationToken cancellationToken = default);
        Task<List<QuestionBank>> GetByIdsWithDetailsAsync(
        IEnumerable<string> questionBankIds,
        CancellationToken cancellationToken = default);

        /// <summary>
        /// Kho - Hàm lấy ngẫu nhiên câu hỏi theo Type và số lượng cần lấy
        /// </summary>
        /// <param name="questionTypeId"></param>
        /// <param name="quantity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 
        Task<List<QuestionBank>> GetRandomQuestionsByTypeAsync(
            string questionTypeId,
            int quantity,
            List<string> excludedIds,
            DifficultyLevel level,
            CancellationToken cancellationToken);
        Task<List<QuestionBank>> GetRandomQuestionsByTypeAsync(
        string questionTypeId,
        int quantity,
        List<string> excludedIds,
        CancellationToken cancellationToken);
        Task<List<QuestionBank>> GetRandomQuestionsForPracticeAsync(
        string questionTypeId,
        int quantity,
        CancellationToken cancellationToken);
        /// <summary>
        /// Kho - dùng thêm câu hỏi hàng loạt
        /// Chủ yếu bên excel import
        /// </summary>
        /// <param name="questions"></param>
        /// <returns></returns>
        Task AddRangeAsync(IEnumerable<QuestionBank> questions);
        /// <summary>
        /// Kho - dùng để check có bị trùng content question bank hay ko
        /// Chủ yếu bên excel import
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        Task<List<string>> GetExistingContentsAsync(List<string> contents);
        /// <summary>
        /// Kho - Lấy danh sách câu hỏi theo QuestionType 
        /// Khác với của Kiệt vì nó đầy đủ Options theo format cần để update examQuestion
        /// </summary>
        /// <param name="questionTypeId"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchTerm"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<(IEnumerable<QuestionBank> Items, int TotalCount)> GetAvailableQuestionsByTypeAsync(
        string questionTypeId,
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken token = default);
        Task<List<QuestionSignatureDTO>> GetQuestionsByTypeAsync(string questionTypeId);
    }
}
