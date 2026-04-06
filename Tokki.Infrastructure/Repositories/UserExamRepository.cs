using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Mappings;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
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
        public async Task<UserExam?> GetByIdAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamQuestions)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.Passage)
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uwa => uwa.Question)
                        .ThenInclude(q => q.QuestionType)
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uwa => uwa.Question)
                        .ThenInclude(q => q.Passage)
                .AsSplitQuery()
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
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
                .AsSplitQuery()
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
                .AsNoTracking()
                .AsSplitQuery()
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
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }
        public async Task<UserExam?> GetListeningDetailAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam).ThenInclude(e => e.ExamTemplate).ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .AsNoTracking()
                .AsSplitQuery()
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
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }

        public async Task<UserExam?> GetWritingDetailAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam).ThenInclude(e => e.ExamTemplate).ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uwa => uwa.Question).ThenInclude(q => q.Passage) 
                .AsNoTracking()
                .AsSplitQuery()
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
                .Where(ua => ua.UserExamId == userExamId && ua.IsCorrect != true) 
                .Select(ua => ua.Question.QuestionTypeId);

            var writingTypeIds = _context.UserExamWritingAnswers
                .Where(uwa => uwa.UserExamId == userExamId)
                .Select(uwa => uwa.Question.QuestionTypeId);

            return await objectiveTypeIds
                .Union(writingTypeIds)
                .Where(id => id != null)
                .Distinct() 
                .Join(_context.QuestionTypes,
                      id => id,
                      qt => qt.QuestionTypeId,
                      (id, qt) => qt)
                .OrderBy(qt => qt.Skill)       
                .ThenBy(qt => qt.OrderIndex)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<List<QuestionTypeDto>> GetExamAnalysisSummaryAsync(string userExamId, CancellationToken cancellationToken)
        {
            // Get all MCQ answers and their correctness
            var mcqAnswers = await _context.UserExamAnswers
                .AsNoTracking()
                .Where(ua => ua.UserExamId == userExamId)
                .Select(ua => new { ua.Question.QuestionTypeId, ua.IsCorrect })
                .ToListAsync(cancellationToken);

            // Get all Writing answers
            var writingAnswers = await _context.UserExamWritingAnswers
                .AsNoTracking()
                .Where(uwa => uwa.UserExamId == userExamId)
                .Select(uwa => new { uwa.Question.QuestionTypeId, uwa.Score })
                .ToListAsync(cancellationToken);

            // Get distinct question type IDs from both MCQ and Writing
            var typeIds = mcqAnswers.Select(a => a.QuestionTypeId)
                .Concat(writingAnswers.Select(a => a.QuestionTypeId))
                .Where(id => id != null)
                .Distinct()
                .ToList();

            // Fetch QuestionType details for these IDs
            var questionTypes = await _context.QuestionTypes
                .AsNoTracking()
                .Where(qt => typeIds.Contains(qt.QuestionTypeId))
                .ToListAsync(cancellationToken);

            var result = new List<QuestionTypeDto>();

            foreach (var qt in questionTypes)
            {
                var mcqs = mcqAnswers.Where(a => a.QuestionTypeId == qt.QuestionTypeId).ToList();
                var writings = writingAnswers.Where(a => a.QuestionTypeId == qt.QuestionTypeId).ToList();

                int totalCount = mcqs.Count + writings.Count;
                if (totalCount == 0) continue;

                // Incorrect count:
                int wrongCount = mcqs.Count(a => a.IsCorrect != true) + writings.Count;

                result.Add(new QuestionTypeDto
                {
                    QuestionTypeId = qt.QuestionTypeId,
                    Code = qt.Code ?? string.Empty,
                    Name = qt.Name,
                    Skill = qt.Skill,
                    IsWeakness = wrongCount > 0,
                    WrongRatio = $"{wrongCount}/{totalCount}"
                });
            }

            return result.OrderBy(r => r.Skill).ThenBy(r => r.QuestionTypeId).ToList();
        }
        public async Task SaveSelfDeclaredLevelAsync(
            string userExamId,
            CurrentTopikLevel level,
            CancellationToken cancellationToken = default)
        {
            var exam = await _context.UserExams
                .FirstOrDefaultAsync(e => e.UserExamId == userExamId, cancellationToken);

            if (exam != null)
            {
                exam.SelfDeclaredLevel = level;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        public async Task<UserExam?> GetByIdWithWritingDetailsAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(u => u.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                            .ThenInclude(tp => tp.QuestionType)
                .Include(u => u.UserExamWritingAnswers)
                    .ThenInclude(wa => wa.Question)
                        .ThenInclude(q => q.QuestionType)
                .FirstOrDefaultAsync(u => u.UserExamId == userExamId, token);
        }

        public async Task<(UserExamStatus Status, int TotalWritingTasks, int GradedWritingTasks)?> GetGradingProgressStatsAsync(string userExamId, CancellationToken token)
        {
            var stats = await _context.UserExams
                .AsNoTracking()
                .Where(ue => ue.UserExamId == userExamId)
                .Select(ue => new
                {
                    ue.Status,
                    TotalWritingTasks = ue.UserExamWritingAnswers.Count(),
                    GradedWritingTasks = ue.UserExamWritingAnswers.Count(wa => wa.GradedAt != null)
                })
                .FirstOrDefaultAsync(token);

            if (stats == null) return null;

            return (stats.Status, stats.TotalWritingTasks, stats.GradedWritingTasks);
        }
        public async Task<CurrentTopikLevel?> GetSelfDeclaredLevelAsync(
            string userExamId,
            CancellationToken cancellationToken = default)
        {
            return await _context.UserExams
                .Where(ue => ue.UserExamId == userExamId)
                .Select(ue => ue.SelfDeclaredLevel)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PagedResult<ExamParticipantDTO>> GetPagedParticipantsByExamIdAsync(
            string examId,
            int pageNumber,
            int pageSize,
            ExamParticipantSortBy sortBy = ExamParticipantSortBy.SubmitTime,
            bool isDescending = true,
            CancellationToken cancellationToken = default)
        {
            // 1. Fetch Template Context
            var templateParts = await _context.Exams
                .Where(e => e.ExamId == examId)
                .SelectMany(e => e.ExamTemplate.TemplateParts)
                .Select(tp => new { tp.Skill, tp.QuestionFrom, tp.QuestionTo, tp.Mark })
                .ToListAsync(cancellationToken);

            var questionMappings = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .Select(eq => new { eq.QuestionBankId, eq.QuestionNo })
                .ToDictionaryAsync(x => x.QuestionBankId, x => x.QuestionNo, cancellationToken);

            // 2. Query UserExams
            var query = _context.UserExams
                .Include(ue => ue.User)
                .AsNoTracking()
                .Where(ue => ue.ExamId == examId && ue.Status == UserExamStatus.Completed);

            // Apply sorting
            query = sortBy switch
            {
                ExamParticipantSortBy.Score => isDescending 
                    ? query.OrderByDescending(ue => ue.Score) 
                    : query.OrderBy(ue => ue.Score),
                _ => isDescending 
                    ? query.OrderByDescending(ue => ue.SubmitTime) 
                    : query.OrderBy(ue => ue.SubmitTime)
            };

            var pagedParticipants = await query.ToPagedListAsync(pageNumber, pageSize);
            var ueIds = pagedParticipants.Items.Select(p => p.UserExamId).ToList();

            // 3. Batch fetch answers
            var allAnswers = await _context.UserExamAnswers
                .Where(ua => ueIds.Contains(ua.UserExamId))
                .Select(ua => new { ua.UserExamId, ua.QuestionId, ua.IsCorrect })
                .ToListAsync(cancellationToken);

            var writingAnswers = await _context.UserExamWritingAnswers
                .Where(uwa => ueIds.Contains(uwa.UserExamId))
                .Select(uwa => new { uwa.UserExamId, uwa.Score })
                .ToListAsync(cancellationToken);

            // 4. Map and Calculate
            var dtos = pagedParticipants.Items.Select(ue => {
                var ueAnswers = allAnswers.Where(a => a.UserExamId == ue.UserExamId).ToList();
                var ueWritings = writingAnswers.Where(w => w.UserExamId == ue.UserExamId).ToList();

                int totalCorrect = ueAnswers.Count(a => a.IsCorrect == true);
                int calculatedScore = 0;
                var skillCounts = new Dictionary<string, int>();

                foreach (var part in templateParts)
                {
                    var skillName = part.Skill.ToString();
                    var correctInPart = ueAnswers.Count(a => 
                        questionMappings.TryGetValue(a.QuestionId, out int qNo) && 
                        qNo >= part.QuestionFrom && qNo <= part.QuestionTo && 
                        a.IsCorrect == true);

                    if (!skillCounts.ContainsKey(skillName)) skillCounts[skillName] = 0;
                    skillCounts[skillName] += correctInPart;
                    calculatedScore += correctInPart * part.Mark;
                }

                calculatedScore += ueWritings.Sum(w => w.Score ?? 0);

                return new ExamParticipantDTO
                {
                    UserExamId = ue.UserExamId,
                    UserEmail = ue.User?.Email ?? "N/A",
                    UserName = ue.User?.FullName ?? "N/A",
                    UserAvatar = ue.User?.AvatarUrl,
                    Score = calculatedScore,
                    TotalCorrectQuestions = totalCorrect,
                    CorrectCountBySkill = skillCounts,
                    SubmitTime = ue.SubmitTime
                };
            }).ToList();

            return PagedResult<ExamParticipantDTO>.Create(dtos, pagedParticipants.TotalCount, pageNumber, pageSize);
        }
    }
}