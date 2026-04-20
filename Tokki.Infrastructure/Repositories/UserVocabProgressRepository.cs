using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;
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
        public async Task<List<string>> GetLearnedVocabIdsByTopicAsync(string userId, string topicId)
        {
            var query = from p in _context.UserVocabProgresses
                        join vt in _context.VocabularyTopics on p.VocabularyId equals vt.VocabularyId
                        where p.UserId == userId && vt.TopicId == topicId
                        select p.VocabularyId;

            return await query.ToListAsync();
        }
        public async Task<List<ReviewItemDTO>> GetDueReviewsAsync(string userId, DateTime compareTime, int limit, CancellationToken cancellationToken = default)
        {
            var query = _context.UserVocabProgresses
                .AsNoTracking()
                .Include(x => x.Vocabulary)
                .Where(x => x.UserId == userId
                        && x.Vocabulary.Status == VocabularyStatus.Active
                        && x.NextReviewAt <= compareTime)
                .OrderBy(x => x.NextReviewAt)
                .Take(limit)                 
                .Select(x => new ReviewItemDTO
                {
                    UserVocabProgressId = x.UserVocabProgressId,
                    VocabularyId = x.VocabularyId,
                    BoxLevel = x.BoxLevel,
                    NextReviewAt = x.NextReviewAt,
                    Streak = x.Streak,
                    Text = x.Vocabulary.Text,
                    Definition = x.Vocabulary.Definition,
                    Pronunciation = x.Vocabulary.Pronunciation,
                    ImageUrl = x.Vocabulary.ImgURL,
                    AudioUrl = x.Vocabulary.AudioURL
                });

            var resultList = await query.ToListAsync(cancellationToken);
            return resultList.OrderBy(x => Guid.NewGuid()).ToList();
        }

        public async Task<PagedResult<ReviewItemDTO>> GetPaginatedDueReviewsAsync(string userId, DateTime compareTime, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var baseQuery = _context.UserVocabProgresses
                .AsNoTracking()
                .Include(x => x.Vocabulary)
                .Where(x => x.UserId == userId
                        && x.Vocabulary.Status == VocabularyStatus.Active
                        && x.NextReviewAt <= compareTime);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderBy(x => x.NextReviewAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ReviewItemDTO
                {
                    UserVocabProgressId = x.UserVocabProgressId,
                    VocabularyId = x.VocabularyId,
                    BoxLevel = x.BoxLevel,
                    NextReviewAt = x.NextReviewAt,
                    Streak = x.Streak,
                    Text = x.Vocabulary.Text,
                    Definition = x.Vocabulary.Definition,
                    Pronunciation = x.Vocabulary.Pronunciation,
                    ImageUrl = x.Vocabulary.ImgURL,
                    AudioUrl = x.Vocabulary.AudioURL
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ReviewItemDTO>(items, totalCount, pageIndex, pageSize);
        }
    }
}
