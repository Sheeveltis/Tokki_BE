using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IExamRepository
    {
        Task<Exam?> GetByIdAsync(string examId, CancellationToken cancellationToken = default);
        Task<Exam?> GetByIdWithDetailsAsync(string examId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<Exam> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamType? type = null,
            ExamStatus? status = null,
            string? examTemplateId = null,
            ExamCreatorFilter creatorFilter = ExamCreatorFilter.All,
            CancellationToken cancellationToken = default);
        Task<bool> IsTitleExistsAsync(string title, string? excludeId = null, CancellationToken cancellationToken = default);
        Task<int> GetQuestionCountAsync(string examId, CancellationToken cancellationToken = default);
        Task AddAsync(Exam exam);
        Task UpdateAsync(Exam exam);
        Task DeleteAsync(Exam exam);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Exam?> GetExamWithFullDetailsAsync(string examId, CancellationToken cancellationToken);
        Task<Exam?> GetEntranceExamByTypeAsync(
            ExamType examType,
            CancellationToken cancellationToken = default);
        Task<List<string>> GetRecentQuestionIdsAsync(int examCount, CancellationToken cancellationToken = default);
    }
}
