using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class PronunciationExampleRepository : IPronunciationExampleRepository
    {
        private readonly TokkiDbContext _context;

        public PronunciationExampleRepository(TokkiDbContext context)
        {
            _context = context;
        }
        public async Task<PronunciationExample?> GetByIdAsync(string id)
        {
            return await _context.PronunciationExamples
                .FirstOrDefaultAsync(x => x.ExampleId == id && !x.IsDeleted);
        }

        public async Task AddRangeAsync(IEnumerable<PronunciationExample> entities)
        {
            await _context.PronunciationExamples.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
    }
}
