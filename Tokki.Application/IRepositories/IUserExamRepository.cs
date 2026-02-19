using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

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
        Task<PagedResult<UserExamActionDto>> GetPagedHistoryAsync(
            string userId,
            string? examId,
            UserExamStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken token);
        Task<UserExamAnswer?> GetMCQAnswerWithSessionAsync(string answerId, CancellationToken token);
        Task<UserExamWritingAnswer?> GetWritingAnswerWithSessionAsync(string answerId, CancellationToken token);
    }
}
