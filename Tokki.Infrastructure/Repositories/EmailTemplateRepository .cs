using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
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
            // Sắp xếp theo CreateAt mới thêm (hoặc UpdatedAt tùy bạn)
            return await _context.EmailTemplates
                .AsNoTracking()
                .OrderByDescending(t => t.CreateAt)
                .ToListAsync();
        }

        public async Task<EmailTemplate?> GetByIdAsync(string id)
        {
            return await _context.EmailTemplates.FindAsync(id);
        }

        // key = TemplateName (giữ tương thích)
        public Task<EmailTemplate?> GetByKeyAsync(string key)
        {
            return GetByNameAsync(key);
        }

        public async Task<EmailTemplate?> GetByNameAsync(string templateName)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateName == templateName);
        }

        public async Task<EmailTemplate?> GetByTypeValueTargetAsync(EmailTemplateType type, int value, UserTargetGroup targetGroup)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Type == type && t.Value == value && t.TargetGroup == targetGroup);
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

        public async Task DeleteAsync(string id)
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
        public async Task<(List<EmailTemplate> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.EmailTemplates.AsNoTracking();

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

    }
}
