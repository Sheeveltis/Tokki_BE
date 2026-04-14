using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IExamTemplateRepository
    {
        Task<ExamTemplate?> GetByIdAsync(string examTemplateId, CancellationToken cancellationToken = default);
        Task<ExamTemplate?> GetByIdWithPartsAsync(string examTemplateId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<ExamTemplate> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamTemplateStatus? status = null,
            CancellationToken cancellationToken = default,
            ExamType? type = null,
            ExamCreatorFilter? creatorFilter = ExamCreatorFilter.All);
        Task<bool> IsNameExistsAsync(string name, string? excludeId = null);
        Task<bool> HasExamsAsync(string examTemplateId, CancellationToken cancellationToken = default);
        Task AddAsync(ExamTemplate examTemplate);
        Task UpdateAsync(ExamTemplate examTemplate);
        Task DeleteAsync(ExamTemplate examTemplate);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);       
    }
}
