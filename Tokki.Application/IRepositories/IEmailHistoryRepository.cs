using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IEmailHistoryRepository
    {
        Task DeleteByUserAndTemplateTypeAsync(string userId, EmailTemplateType type, CancellationToken ct);
    }
}
