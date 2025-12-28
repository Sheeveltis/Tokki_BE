using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class VocabularyExampleRepository : IVocabularyExampleRepository
    {
        private readonly TokkiDbContext _context;

        public VocabularyExampleRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<VocabularyExample?> GetByIdAsync(string exampleId)
        {
            return await _context.VocabularyExamples
                .Include(e => e.Vocabulary)
                .FirstOrDefaultAsync(e => e.ExampleId == exampleId);
        }

        public async Task<List<VocabularyExample>> GetByVocabularyIdAsync(string vocabularyId)
        {
            return await _context.VocabularyExamples
                .Where(e => e.VocabularyId == vocabularyId && e.Status == VocabularyExampleStatus.Active)
                .OrderBy(e => e.CreateAt)
                .ToListAsync();
        }

        public async Task<VocabularyExample?> GetBySentenceAsync(string vocabularyId, string sentence)
        {
            return await _context.VocabularyExamples
                .FirstOrDefaultAsync(e =>
                    e.VocabularyId == vocabularyId &&
                    e.Sentence == sentence &&
                    e.Status == VocabularyExampleStatus.Active);
        }

        public async Task AddAsync(VocabularyExample example)
        {
            await _context.VocabularyExamples.AddAsync(example);
        }

        public async Task AddRangeAsync(List<VocabularyExample> examples)
        {
            await _context.VocabularyExamples.AddRangeAsync(examples);
        }

        public async Task UpdateAsync(VocabularyExample example)
        {
            _context.VocabularyExamples.Update(example);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(VocabularyExample example)
        {
            _context.VocabularyExamples.Remove(example);
            await Task.CompletedTask;
        }

        public async Task DeleteRangeAsync(List<VocabularyExample> examples)
        {
            _context.VocabularyExamples.RemoveRange(examples);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }
    }
}