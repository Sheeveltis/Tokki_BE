// Tokki.Application/IRepositories/IEmailTemplateRepository.cs
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IEmailTemplateRepository
    {
        Task<List<EmailTemplate>> GetAllAsync();
        Task<EmailTemplate?> GetByIdAsync(string id); 
        Task<EmailTemplate?> GetByKeyAsync(string key); 
        Task AddAsync(EmailTemplate template);
        Task UpdateAsync(EmailTemplate template);
        Task DeleteAsync(string id); 
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}