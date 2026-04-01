using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class ExamRepository : IExamRepository
    {
        private readonly TokkiDbContext _context;

        public ExamRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Exam?> GetByIdAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.Exams
                .FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        }

        public async Task<Exam?> GetByIdWithDetailsAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.Exams
                .Include(e => e.ExamTemplate)
                    .ThenInclude(et => et.TemplateParts)
                .Include(e => e.ExamQuestions.OrderBy(eq => eq.QuestionNo))
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionOptions)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionType)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.Passage)
                .FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        }

        public async Task<(IEnumerable<Exam> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamType? type = null,
            ExamStatus? status = null,
            string? examTemplateId = null,
            ExamCreatorFilter creatorFilter = ExamCreatorFilter.All,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Exams
                .Include(e => e.ExamTemplate)
                .Include(e => e.ExamQuestions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.Title.Contains(searchTerm));
            }

            if (type.HasValue)
            {
                query = query.Where(e => e.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(examTemplateId))
            {
                query = query.Where(e => e.ExamTemplateId == examTemplateId);
            }

            if (creatorFilter == ExamCreatorFilter.AI)
            {
                query = query.Where(e => e.CreatedBy == "AI_EXAM_SYSTEM");
            }
            else if (creatorFilter == ExamCreatorFilter.Human)
            {
                query = query.Where(e => e.CreatedBy != "AI_EXAM_SYSTEM");
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IEnumerable<ExamStatProjection> items, int totalCount)> GetPagedWithStatsAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamType? type = null,
            ExamStatus? status = null,
            ExamCreatorFilter creatorFilter = ExamCreatorFilter.All,
            ExamStatsSortBy sortBy = ExamStatsSortBy.CreatedAt,
            bool isDescending = true,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Exams.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.Title.Contains(searchTerm));
            }

            if (type.HasValue)
            {
                query = query.Where(e => e.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            if (creatorFilter == ExamCreatorFilter.AI)
            {
                query = query.Where(e => e.CreatedBy == "AI_EXAM_SYSTEM");
            }
            else if (creatorFilter == ExamCreatorFilter.Human)
            {
                query = query.Where(e => e.CreatedBy != "AI_EXAM_SYSTEM");
            }

            // Apply sorting (BEFORE paging)
            query = sortBy switch
            {
                ExamStatsSortBy.Participants => isDescending 
                    ? query.OrderByDescending(e => e.UserExams.Count()) 
                    : query.OrderBy(e => e.UserExams.Count()),
                ExamStatsSortBy.PdfDownload => isDescending 
                    ? query.OrderByDescending(e => e.PdfDownloadCount) 
                    : query.OrderBy(e => e.PdfDownloadCount),
                ExamStatsSortBy.AverageScore => isDescending 
                    ? query.OrderByDescending(e => e.UserExams.Where(ue => ue.Status == UserExamStatus.Completed).Average(ue => (double?)ue.Score) ?? 0)
                    : query.OrderBy(e => e.UserExams.Where(ue => ue.Status == UserExamStatus.Completed).Average(ue => (double?)ue.Score) ?? 0),
                _ => isDescending 
                    ? query.OrderByDescending(e => e.CreatedAt) 
                    : query.OrderBy(e => e.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            // 1. Fetch base exam info first (Fast)
            var baseExams = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExamStatProjection
                {
                    ExamId = e.ExamId,
                    ExamTemplateId = e.ExamTemplateId,
                    ExamTemplateName = e.ExamTemplate != null ? e.ExamTemplate.Name : "N/A",
                    Title = e.Title,
                    Type = e.Type,
                    Status = e.Status,
                    Duration = e.Duration,
                    SkillDurations = e.SkillDurations,
                    CreatedAt = e.CreatedAt,
                    PdfDownloadCount = e.PdfDownloadCount,
                    TemplateParts = e.ExamTemplate != null 
                        ? e.ExamTemplate.TemplateParts.Select(tp => new TemplatePartStatProjection
                        {
                            Skill = tp.Skill,
                            QuestionFrom = tp.QuestionFrom,
                            QuestionTo = tp.QuestionTo,
                            Mark = tp.Mark
                        }).ToList() 
                        : new List<TemplatePartStatProjection>()
                })
                .ToListAsync(cancellationToken);

            if (!baseExams.Any()) return (baseExams, totalCount);

            var examIds = baseExams.Select(e => e.ExamId).ToList();

            // 2. Fetch User Stats in one batch query per page (Very Fast)
            var userStats = await _context.UserExams
                .Where(ue => examIds.Contains(ue.ExamId))
                .GroupBy(ue => ue.ExamId)
                .Select(g => new {
                    ExamId = g.Key,
                    TotalParticipants = g.Count(),
                    AverageScore = g.Where(ue => ue.Status == UserExamStatus.Completed).Average(ue => (double?)ue.Score) ?? 0,
                    TopScore = g.Where(ue => ue.Status == UserExamStatus.Completed).Max(ue => (int?)ue.Score) ?? 0,
                    AverageDurationMinutes = g.Where(ue => ue.Status == UserExamStatus.Completed && ue.SubmitTime != null)
                        .Average(ue => (double?)EF.Functions.DateDiffMinute(ue.StartTime, ue.SubmitTime)) ?? 0,
                    InProgressCount = g.Count(ue => ue.Status == UserExamStatus.InProgress),
                    CompletedCount = g.Count(ue => ue.Status == UserExamStatus.Completed)
                })
                .ToDictionaryAsync(x => x.ExamId, x => x, cancellationToken);

            // 3. Fetch Question Info in one batch query per page (Very Fast)
            var questionStats = await _context.ExamQuestions
                .Where(eq => examIds.Contains(eq.ExamId))
                .GroupBy(eq => eq.ExamId)
                .Select(g => new {
                    ExamId = g.Key,
                    TotalQuestions = g.Count(),
                    QuestionNumbers = g.Select(eq => eq.QuestionNo).ToList()
                })
                .ToDictionaryAsync(x => x.ExamId, x => x, cancellationToken);

            // 4. Merge results
            foreach (var exam in baseExams)
            {
                if (userStats.TryGetValue(exam.ExamId, out var us))
                {
                    exam.TotalParticipants = us.TotalParticipants;
                    exam.AverageScore = us.AverageScore;
                    exam.TopScore = us.TopScore;
                    exam.AverageDurationMinutes = us.AverageDurationMinutes;
                    exam.InProgressCount = us.InProgressCount;
                    exam.CompletedCount = us.CompletedCount;
                }

                if (questionStats.TryGetValue(exam.ExamId, out var qs))
                {
                    exam.TotalQuestions = qs.TotalQuestions;
                    exam.QuestionNumbers = qs.QuestionNumbers;
                    
                    // Final calculation of MaxScore in memory
                    exam.MaxScore = exam.TemplateParts.Sum(tp => 
                        qs.QuestionNumbers.Count(qNo => qNo >= tp.QuestionFrom && qNo <= tp.QuestionTo) * tp.Mark);
                }
            }

            return (baseExams, totalCount);
        }

        public async Task<ExamStatProjection?> GetExamStatsByIdAsync(string examId, CancellationToken cancellationToken = default)
        {
            var exam = await _context.Exams.AsNoTracking()
                .Where(e => e.ExamId == examId)
                .Select(e => new ExamStatProjection
                {
                    ExamId = e.ExamId,
                    ExamTemplateId = e.ExamTemplateId,
                    ExamTemplateName = e.ExamTemplate != null ? e.ExamTemplate.Name : "N/A",
                    Title = e.Title,
                    Type = e.Type,
                    Status = e.Status,
                    Duration = e.Duration,
                    SkillDurations = e.SkillDurations,
                    CreatedAt = e.CreatedAt,
                    PdfDownloadCount = e.PdfDownloadCount,
                    TemplateParts = e.ExamTemplate != null 
                        ? e.ExamTemplate.TemplateParts.Select(tp => new TemplatePartStatProjection
                        {
                            Skill = tp.Skill,
                            QuestionFrom = tp.QuestionFrom,
                            QuestionTo = tp.QuestionTo,
                            Mark = tp.Mark
                        }).ToList() 
                        : new List<TemplatePartStatProjection>()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (exam == null) return null;

            // Stats
            var userStats = await _context.UserExams
                .Where(ue => ue.ExamId == examId)
                .GroupBy(ue => ue.ExamId)
                .Select(g => new {
                    TotalParticipants = g.Count(),
                    AverageScore = g.Where(ue => ue.Status == UserExamStatus.Completed).Average(ue => (double?)ue.Score) ?? 0,
                    TopScore = g.Where(ue => ue.Status == UserExamStatus.Completed).Max(ue => (int?)ue.Score) ?? 0,
                    AverageDurationMinutes = g.Where(ue => ue.Status == UserExamStatus.Completed && ue.SubmitTime != null)
                        .Average(ue => (double?)EF.Functions.DateDiffMinute(ue.StartTime, ue.SubmitTime)) ?? 0,
                    InProgressCount = g.Count(ue => ue.Status == UserExamStatus.InProgress),
                    CompletedCount = g.Count(ue => ue.Status == UserExamStatus.Completed)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (userStats != null)
            {
                exam.TotalParticipants = userStats.TotalParticipants;
                exam.AverageScore = userStats.AverageScore;
                exam.TopScore = userStats.TopScore;
                exam.AverageDurationMinutes = userStats.AverageDurationMinutes;
                exam.InProgressCount = userStats.InProgressCount;
                exam.CompletedCount = userStats.CompletedCount;
            }

            var questions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .Select(eq => eq.QuestionNo)
                .ToListAsync(cancellationToken);

            exam.TotalQuestions = questions.Count;
            exam.QuestionNumbers = questions;
            
            // Final calculation in memory
            exam.MaxScore = exam.TemplateParts.Sum(tp => 
                questions.Count(qNo => qNo >= tp.QuestionFrom && qNo <= tp.QuestionTo) * tp.Mark);

            return exam;
        }

        public async Task<bool> IsTitleExistsAsync(string title, string? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Exams.AsNoTracking() 
                              .Where(e => e.Title == title);

            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(e => e.ExamId != excludeId);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<int> GetQuestionCountAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamQuestions
                .Where(eq => examId.Contains(eq.ExamId))
                .CountAsync(cancellationToken);
        }

        public async Task AddAsync(Exam exam)
        {
            await _context.Exams.AddAsync(exam);
        }

        public Task UpdateAsync(Exam exam)
        {
            _context.Exams.Update(exam);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Exam exam)
        {
            _context.Exams.Remove(exam);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<Exam?> GetExamWithFullDetailsAsync(string examId, CancellationToken cancellationToken)
        {
            return await _context.Exams
                .AsNoTracking()
                .AsSplitQuery()
                .Include(e => e.ExamTemplate)
                    .ThenInclude(t => t.TemplateParts)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionOptions) 
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.Passage)         
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionType)   
                .FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        }

        public async Task<Exam?> GetEntranceExamByTypeAsync(
            ExamType examType,
            CancellationToken cancellationToken = default)
        {
            return await _context.Exams
                .Where(e => e.Type == examType
                         && e.Status == ExamStatus.Published)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<string>> GetRecentQuestionIdsAsync(int examCount, CancellationToken cancellationToken = default)
        {
            return await _context.Exams
                .OrderByDescending(e => e.CreatedAt)
                .Take(examCount)
                .SelectMany(e => e.ExamQuestions)
                .Select(eq => eq.QuestionBankId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
    }
}
