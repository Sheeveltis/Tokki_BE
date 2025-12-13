using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        Task AddAsync(TemplatePart templatePart);
        Task AddRangeAsync(IEnumerable<TemplatePart> templateParts);
        Task UpdateAsync(TemplatePart templatePart);
        Task DeleteAsync(TemplatePart templatePart);
        Task DeleteRangeAsync(IEnumerable<TemplatePart> templateParts);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
