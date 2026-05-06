using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
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

        public async Task<(List<(DailyWordle Item, bool IsLocked)> Items, int TotalCount)> GetPagedDailyWordlesAsync(int pageNumber, int pageSize, DateOnly? date, WordleLevel? level, string? searchTerm, CancellationToken token)
        {
            var query = _context.DailyWordles
                .Include(dw => dw.Vocabulary)
                .AsQueryable();

            if (date.HasValue)
            {
                query = query.Where(dw => dw.GameDate == date.Value);
            }

            if (level.HasValue)
            {
                query = query.Where(dw => dw.Level == level.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(dw => dw.Word.Contains(searchTerm) || dw.Vocabulary.Definition.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(token);
            var items = await query
                .OrderByDescending(dw => dw.GameDate)
                .ThenBy(dw => dw.Level)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(dw => new 
                {
                    DailyWordle = dw,
                    IsLocked = _context.UserWordleProgress.Any(p => p.DailyWordleId == dw.DailyWordleId)
                })
                .ToListAsync(token);

            var resultItems = items.Select(x => (Item: x.DailyWordle, IsLocked: x.IsLocked)).ToList();

            return (resultItems, totalCount);
        }

        public async Task UpdateDailyWordleAsync(DailyWordle dailyWordle, CancellationToken token)
        {
            _context.DailyWordles.Update(dailyWordle);
            await _context.SaveChangesAsync(token);
        }

        public async Task<DailyWordle?> GetDailyWordleByIdAsync(string dailyWordleId, CancellationToken token)
        {
            return await _context.DailyWordles.FirstOrDefaultAsync(dw => dw.DailyWordleId == dailyWordleId, token);
        }

        public async Task<Tokki.Domain.Entities.Vocabulary?> GetRandomVocabularyByLengthAsync(int length, CancellationToken token)
        {
            return await _context.Vocabularies
                .Where(v => v.Text.Length == length && !v.Text.Contains(" "))
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefaultAsync(token);
        }

        public async Task<Tokki.Domain.Entities.Vocabulary?> GetVocabularyByIdAsync(string vocabularyId, CancellationToken token)
        {
            return await _context.Vocabularies.FirstOrDefaultAsync(v => v.VocabularyId == vocabularyId, token);
        }

        public async Task<(List<Tokki.Domain.Entities.Vocabulary> Items, int TotalCount)> GetPagedSuitableVocabsAsync(int length, int pageNumber, int pageSize, string? searchTerm, CancellationToken token)
        {
            var query = _context.Vocabularies
                .Where(v => v.Text.Length == length && !v.Text.Contains(" "))
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(v => v.Text.Contains(searchTerm) || v.Definition.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(token);
            var items = await query
                .OrderBy(v => v.Text)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return (items, totalCount);
        }

        public async Task<bool> AnyUserProgressAsync(string dailyWordleId, CancellationToken token)
        {
            return await _context.UserWordleProgress.AnyAsync(p => p.DailyWordleId == dailyWordleId, token);
        }
    }
}
