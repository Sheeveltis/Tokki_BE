using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IExamQuestionRepository
    {
        Task<ExamQuestion?> GetByIdAsync(string examQuestionId, CancellationToken cancellationToken = default);
        Task<ExamQuestion?> GetByExamAndQuestionNoAsync(string examId, int questionNo, CancellationToken cancellationToken = default);
        Task<IEnumerable<ExamQuestion>> GetByExamIdAsync(string examId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ExamQuestion>> GetByExamIdWithDetailsAsync(string examId, CancellationToken cancellationToken = default);
        Task<bool> IsQuestionNoExistsAsync(string examId, int questionNo, string? excludeId = null);
        Task<int> GetMaxQuestionNoAsync(string examId, CancellationToken cancellationToken = default);
        Task AddAsync(ExamQuestion examQuestion);
        Task AddRangeAsync(IEnumerable<ExamQuestion> examQuestions);
        Task UpdateAsync(ExamQuestion examQuestion);
        Task DeleteAsync(ExamQuestion examQuestion);
        Task DeleteRangeAsync(IEnumerable<ExamQuestion> examQuestions);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
