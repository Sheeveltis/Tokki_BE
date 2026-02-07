using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class EmailHistoryRepository : IEmailHistoryRepository
    {
        private readonly TokkiDbContext _context;

        public EmailHistoryRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task DeleteByUserAndTemplateTypeAsync(string userId, EmailTemplateType type, CancellationToken ct)
        {
            await _context.EmailHistories
                .Where(e => e.UserId == userId && e.EmailTemplate != null && e.EmailTemplate.Type == type)
                .ExecuteDeleteAsync(ct);
        }
    }
}
