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
    }
}
