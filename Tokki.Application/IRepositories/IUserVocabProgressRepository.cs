using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IUserVocabProgressRepository
    {
        Task<UserVocabProgress?> GetByVocabIdAsync(string userId, string vocabularyId, CancellationToken cancellationToken);

        Task AddAsync(UserVocabProgress progress, CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
