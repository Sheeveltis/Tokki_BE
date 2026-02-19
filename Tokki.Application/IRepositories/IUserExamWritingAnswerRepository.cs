// Application/IRepositories/IUserExamWritingAnswerRepository.cs
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserExamWritingAnswerRepository
    {
        Task AddAsync(UserExamWritingAnswer answer, CancellationToken ct = default);
        Task<UserExamWritingAnswer?> GetByIdAsync(string userExamWritingAnswerId, CancellationToken ct = default);
        Task<UserExamWritingAnswer?> GetByExamAndOrderAsync(string userExamId, int orderIndex, CancellationToken ct = default);
        Task<List<UserExamWritingAnswer>> GetByUserExamIdAsync(string userExamId, CancellationToken ct = default);
        Task UpdateAsync(UserExamWritingAnswer answer);
        Task<bool> SaveChangesAsync(CancellationToken ct = default);
        Task<double> GetMaxMarkByOrderIndexAsync(string userExamId, int orderIndex, CancellationToken ct = default);

    }
}