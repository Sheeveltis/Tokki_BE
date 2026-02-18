using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Tokki.Infrastructure.Repositories
{
    public class PronunciationRuleRepository : IPronunciationRuleRepository
    {
        private readonly TokkiDbContext _context;
        public PronunciationRuleRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PronunciationRule rule)
        {
            await _context.PronunciationRules.AddAsync(rule);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PronunciationRule?> GetByIdAsync(string id)
        {
            return await _context.PronunciationRules
                .FirstOrDefaultAsync(r => r.PronunciationRuleId == id && !r.IsDeleted);
        }

        public async Task<bool> IsRuleNameExistsAsync(string ruleName)
        {
            return await _context.PronunciationRules
                .AnyAsync(r => r.RuleName == ruleName && !r.IsDeleted);
        }
    }
}
