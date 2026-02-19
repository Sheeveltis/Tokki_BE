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
    public class WordleRepository : IWordleRepository
    {
        private readonly TokkiDbContext _context;

        public WordleRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<DailyWordle?> GetDailyWordleWithVocabAsync(string dailyWordleId, CancellationToken token)
        {
            return await _context.DailyWordles
                .Include(dw => dw.Vocabulary)
                .FirstOrDefaultAsync(dw => dw.DailyWordleId == dailyWordleId, token);
        }

        public async Task AddSubmissionAsync(WordleSentenceSubmission submission, CancellationToken token)
        {
            await _context.WordleSentenceSubmissions.AddAsync(submission, token);
            await _context.SaveChangesAsync(token);
        }

        public async Task<WordleSentenceSubmission?> GetSubmissionByIdAsync(string submissionId, CancellationToken token)
        {
            return await _context.WordleSentenceSubmissions
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, token);
        }

        public async Task UpdateSubmissionAsync(WordleSentenceSubmission submission, CancellationToken token)
        {
            _context.WordleSentenceSubmissions.Update(submission);
            await _context.SaveChangesAsync(token);
        }
    }
}
