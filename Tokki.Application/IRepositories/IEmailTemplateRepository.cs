using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IEmailTemplateRepository
    {
        Task<List<EmailTemplate>> GetAllAsync();
        Task<EmailTemplate?> GetByIdAsync(int id);
        Task<EmailTemplate?> GetByKeyAsync(string key);
        Task AddAsync(EmailTemplate template);
        Task UpdateAsync(EmailTemplate template);
        Task DeleteAsync(int id);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
