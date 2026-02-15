using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserExamRepository
    {
        Task<UserExam?> GetByIdAsync(string userExamId, CancellationToken token);
        Task<UserExam?> GetInProgressSessionAsync(string userId, string examId, CancellationToken token);

        Task<Exam?> GetExamWithFullStructureAsync(string examId, CancellationToken token);

        Task AddSessionAsync(UserExam session, CancellationToken token);
        Task SaveChangesAsync(CancellationToken token);
        Task<UserExam?> GetReviewByIdAsync(string userExamId, CancellationToken token);
        Task<List<UserExam>> GetExpiredSessionsAsync(CancellationToken token);
        Task<UserExam?> GetByAnswerIdAsync(string userAnswerId, CancellationToken token);
    }
}
