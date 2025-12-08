// Tokki.Infrastructure/Repositories/EmailTemplateRepository.cs
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class EmailTemplateRepository : IEmailTemplateRepository
    {
        private readonly TokkiDbContext _context;

        public EmailTemplateRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmailTemplate>> GetAllAsync()
        {
            return await _context.EmailTemplates
                .OrderBy(t => t.TemplateKey)
                .ToListAsync();
        }

        public async Task<EmailTemplate?> GetByIdAsync(int id)
        {
            return await _context.EmailTemplates.FindAsync(id);
        }

        public async Task<EmailTemplate?> GetByKeyAsync(string key)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateKey == key);
        }

        public async Task AddAsync(EmailTemplate template)
        {
            await _context.EmailTemplates.AddAsync(template);
        }

        public Task UpdateAsync(EmailTemplate template)
        {
            _context.EmailTemplates.Update(template);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var template = await GetByIdAsync(id);
            if (template != null)
            {
                _context.EmailTemplates.Remove(template);
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}