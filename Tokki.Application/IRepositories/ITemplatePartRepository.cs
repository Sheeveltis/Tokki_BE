using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface ITemplatePartRepository
    {
        Task<TemplatePart?> GetByIdAsync(string templatePartId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TemplatePart>> GetByTemplateIdAsync(string examTemplateId, CancellationToken cancellationToken = default);
        Task<bool> IsQuestionRangeOverlapAsync(string examTemplateId, int questionFrom, int questionTo, string? excludePartId = null);
        Task<TemplatePart?> GetPartByQuestionNoAsync(string examTemplateId, int questionNo, CancellationToken cancellationToken = default);
        Task<(int totalParts, int totalQuestions)> GetStatsByTemplateIdAsync(string examTemplateId);
        Task AddAsync(TemplatePart templatePart);
        Task AddRangeAsync(IEnumerable<TemplatePart> templateParts);
        Task UpdateAsync(TemplatePart templatePart);
        Task DeleteAsync(TemplatePart templatePart);
        Task DeleteRangeAsync(IEnumerable<TemplatePart> templateParts);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<(IEnumerable<TemplatePart> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? examTemplateId = null,
            CancellationToken cancellationToken = default);
    }
}