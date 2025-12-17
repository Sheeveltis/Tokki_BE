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
    public class UserVocabProgressRepository : IUserVocabProgressRepository
    {
        private readonly TokkiDbContext _context;

        public UserVocabProgressRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserVocabProgress?> GetByVocabIdAsync(string userId, string vocabularyId, CancellationToken cancellationToken)
        {
            return await _context.UserVocabProgresses.FirstOrDefaultAsync(x => x.UserId == userId && x.VocabularyId == vocabularyId, cancellationToken);
        }

        public async Task AddAsync(UserVocabProgress progress, CancellationToken cancellationToken)
        {
            await _context.UserVocabProgresses.AddAsync(progress, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
