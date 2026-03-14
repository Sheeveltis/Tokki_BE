using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Mappings;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserExamRepository : IUserExamRepository
    {
        private readonly TokkiDbContext _context;

        public UserExamRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserExam?> GetInProgressSessionAsync(string userId, string examId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(t => t.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .Include(ue => ue.UserExamWritingAnswers) 
                    .ThenInclude(wa => wa.Question)
                .FirstOrDefaultAsync(ue => ue.UserId == userId
                                        && ue.ExamId == examId
                                        && ue.Status == UserExamStatus.InProgress, token);
        }

        public async Task<Exam?> GetExamWithFullStructureAsync(string examId, CancellationToken token)
        {
            return await _context.Exams
                .Include(e => e.ExamTemplate)
                    .ThenInclude(t => t.TemplateParts)

                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionOptions)

                .FirstOrDefaultAsync(e => e.ExamId == examId, token);
        }

        public async Task AddSessionAsync(UserExam session, CancellationToken token)
        {
            await _context.UserExams.AddAsync(session, token);
            await _context.SaveChangesAsync(token);
        }
        public async Task SaveChangesAsync(CancellationToken token)
        {
            await _context.SaveChangesAsync(token);
        }
        public async Task<UserExam?> GetByIdAsync(string id, CancellationToken token)
        {
            return await _context.UserExams
                .AsNoTracking()
                .AsSplitQuery()
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions) 
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uw => uw.Question) 
                .FirstOrDefaultAsync(x => x.UserExamId == id, token);
        }
        public async Task<UserExam?> GetReviewByIdAsync(string userExamId, CancellationToken cancellationToken)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uwa => uwa.Question)
                .AsNoTracking() 
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, cancellationToken);
        }
        public async Task<List<UserExam>> GetExpiredSessionsAsync(CancellationToken token)
        {
            var now = DateTime.UtcNow;
            return await _context.UserExams
                .Include(ue => ue.Exam)
                .Where(ue => ue.Status == UserExamStatus.InProgress &&
                             ue.StartTime.AddMinutes(ue.Exam.Duration + 2) < now) 
                .ToListAsync(token);
        }
        public async Task<PagedResult<UserExamActionDto>> GetPagedHistoryAsync(
         string userId,
         string? examId,
         UserExamStatus? status,
         int pageNumber,
         int pageSize,
         CancellationToken token)
        {
            var query = _context.UserExams
                .AsNoTracking()
                .Where(ue => ue.UserId == userId);

            if (!string.IsNullOrEmpty(examId))
                query = query.Where(ue => ue.ExamId == examId);

            if (status.HasValue)
                query = query.Where(ue => ue.Status == status.Value);

            var dtoQuery = query
                .OrderByDescending(ue => ue.StartTime)
                .Select(ue => new UserExamActionDto
                {
                    UserExamId = ue.UserExamId,
                    ExamId = ue.ExamId,
                    ExamTitle = ue.Exam.Title,
                    Status = ue.Status.ToString(),
                    LastAttempt = ue.SubmitTime ?? ue.StartTime,
                    TimeRemaining = ue.Status == UserExamStatus.InProgress
                        ? (ue.Exam.Duration * 60) - (int)EF.Functions.DateDiffSecond(ue.StartTime, DateTime.UtcNow)
                        : 0
                });

            return await dtoQuery.ToPagedListAsync(pageNumber, pageSize);
        }
        public async Task<List<UserExamAnswer>> GetMCQAnswersByIdsAsync(List<string> ids, CancellationToken token)
        {
            return await _context.UserExamAnswers
                .Include(uq => uq.UserExam) 
                .Include(uq => uq.Question)
                    .ThenInclude(q => q.QuestionOptions)
                .Where(uq => ids.Contains(uq.UserExamAnswerId))
                .ToListAsync(token);
        }
        public async Task<UserExamWritingAnswer?> GetWritingAnswerWithSessionAsync(string answerId, CancellationToken token)
        {
            return await _context.UserExamWritingAnswers
                .Include(w => w.UserExam)
                .FirstOrDefaultAsync(w => w.UserExamWritingAnswerId == answerId, token);
        }
        public async Task<UserExam?> GetResultWithDetailsAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.User)
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                .Include(ue => ue.UserExamWritingAnswers)
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }
        public async Task<UserExam?> GetSkillDetailResultAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }
        public async Task<UserExam?> GetListeningDetailAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam).ThenInclude(e => e.ExamTemplate).ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }

        public async Task<UserExam?> GetReadingDetailAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam).ThenInclude(e => e.ExamTemplate).ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question).ThenInclude(q => q.QuestionOptions)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question).ThenInclude(q => q.Passage) 
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }

        public async Task<UserExam?> GetWritingDetailAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam).ThenInclude(e => e.ExamTemplate).ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uwa => uwa.Question).ThenInclude(q => q.Passage) 
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }
        public async Task<bool> HasPendingWritingAnswersAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExamWritingAnswers
                .AnyAsync(a => a.UserExamId == userExamId && a.Score == null, token);
        }
        public async Task<List<QuestionType>> GetIncorrectQuestionTypesByExamIdAsync(string userExamId, CancellationToken cancellationToken)
        {
            var objectiveTypeIds = _context.UserExamAnswers
                .Where(ua => ua.UserExamId == userExamId && ua.IsCorrect == false)
                .Select(ua => ua.Question.QuestionTypeId);

            var writingTypeIds = _context.UserExamWritingAnswers
                .Where(uwa => uwa.UserExamId == userExamId)
                .Select(uwa => uwa.Question.QuestionTypeId);
            return await objectiveTypeIds
                .Union(writingTypeIds)
                .Join(_context.QuestionTypes,
                      id => id,
                      qt => qt.QuestionTypeId,
                      (id, qt) => qt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}