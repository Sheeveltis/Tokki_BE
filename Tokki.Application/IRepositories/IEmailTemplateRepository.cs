using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IEmailTemplateRepository
    {
        Task<List<EmailTemplate>> GetAllAsync();
        Task<EmailTemplate?> GetByIdAsync(string id);

        // Giữ tên cũ để đỡ sửa nhiều code: key = TemplateName
        Task<EmailTemplate?> GetByKeyAsync(string key);

        // Khuyến nghị dùng tên rõ ràng
        Task<EmailTemplate?> GetByNameAsync(string templateName);

        // Check trùng theo unique index (Type + Value + TargetGroup)
        Task<EmailTemplate?> GetByTypeValueTargetAsync(EmailTemplateType type, int value, UserTargetGroup targetGroup);

        Task AddAsync(EmailTemplate template);
        Task UpdateAsync(EmailTemplate template);
        Task DeleteAsync(string id);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<(List<EmailTemplate> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);

    }
}
