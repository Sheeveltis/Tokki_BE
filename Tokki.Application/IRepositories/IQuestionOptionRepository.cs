using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IQuestionOptionRepository
    {
        Task<QuestionOption?> GetByIdAsync(string optionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<QuestionOption>> GetByQuestionBankIdAsync(string questionBankId, CancellationToken cancellationToken = default);
        Task<QuestionOption?> GetCorrectOptionAsync(string questionBankId, CancellationToken cancellationToken = default);
        Task AddAsync(QuestionOption questionOption);
        Task AddRangeAsync(IEnumerable<QuestionOption> questionOptions);
        Task UpdateAsync(QuestionOption questionOption);
        Task DeleteAsync(QuestionOption questionOption);
        Task DeleteRangeAsync(IEnumerable<QuestionOption> questionOptions);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
