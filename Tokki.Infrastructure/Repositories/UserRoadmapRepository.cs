using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserRoadmapRepository : IUserRoadmapRepository
    {
        private readonly TokkiDbContext _context;

        public UserRoadmapRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserRoadmap roadmap)
        {
            await _context.UserRoadmaps.AddAsync(roadmap);
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
        public async Task<UserRoadmap?> GetActiveRoadmapByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var roadmap = await _context.UserRoadmaps
                .Include(r => r.Weeks)
                .ThenInclude(w => w.DailyTasks)
                .ThenInclude(t => t.QuestionType)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CurrentStatus == UserRoadmapStatus.Active, cancellationToken);

            if (roadmap != null)
            {
                foreach (var week in roadmap.Weeks)
                {
                    week.DailyTasks = week.DailyTasks
                        .OrderBy(t => t.DayIndex)
                        .ThenBy(t => (int)t.TaskType)
                        .ToList();
                }
            }

            return roadmap;
        }
        public async Task<RoadmapDailyTask?> GetTaskByIdAsync(string taskId, CancellationToken cancellationToken = default)
        {
            return await _context.RoadmapDailyTasks
                .Include(t => t.RoadmapWeek)
                .ThenInclude(w => w.UserRoadmap)
                .Include(t => t.QuestionType)
                .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);
        }
        public async Task<RoadmapWeek?> GetWeekByIdAsync(string weekId, CancellationToken cancellationToken)
        {
            return await _context.RoadmapWeeks
                .Include(w => w.UserRoadmap)
                .Include(w => w.WeeklyExam)
                .Include(x => x.DailyTasks)
                .FirstOrDefaultAsync(w => w.RoadmapWeekId == weekId, cancellationToken);
        }

        public async Task<RoadmapWeek?> GetWeekByIndexAsync(string roadmapId, int weekIndex, CancellationToken cancellationToken)
        {
            return await _context.RoadmapWeeks
                .Include(w => w.DailyTasks)
                .FirstOrDefaultAsync(w => w.UserRoadmapId == roadmapId && w.WeekIndex == weekIndex, cancellationToken);
        }
        public async Task<int> GetExamScoreAsync(string examId, string userId, CancellationToken cancellationToken = default)
        {
            var score = await _context.UserExams
                .Where(ue => ue.ExamId == examId && ue.UserId == userId && ue.Status == UserExamStatus.Completed)
                .OrderByDescending(ue => ue.SubmitTime)
                .Select(ue => ue.Score)
                .FirstOrDefaultAsync(cancellationToken);

            return score;
        }
        public async Task<List<string>> GetWeakQuestionTypesFromExamAsync(string userExamId, CancellationToken cancellationToken = default)
        {
            var weakTypes = await _context.UserExamAnswers
                .Where(d => d.UserExamId == userExamId && d.IsCorrect == false)
                .Include(d => d.Question)
                .GroupBy(d => d.Question.QuestionTypeId)
                .Select(g => new { TypeId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(3)
                .Select(x => x.TypeId)
                .ToListAsync(cancellationToken);

            return weakTypes;
        }
        public async Task<UserExam?> GetUserExamByExamIdAsync(string examId, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserExams
                .Where(ue => ue.ExamId == examId && ue.UserId == userId && ue.Status == UserExamStatus.Completed)
                .OrderByDescending(ue => ue.SubmitTime)
                .FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<List<ExamQuestion>> GetExamQuestionsForGradingAsync(string examId, CancellationToken cancellationToken = default)
        {
            return await _context.ExamQuestions
                .Include(eq => eq.QuestionBank)
                    .ThenInclude(qb => qb.QuestionOptions)
                .Where(eq => eq.ExamId == examId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddUserExamAsync(UserExam userExam)
        {
            await _context.UserExams.AddAsync(userExam);
        }

        public async Task AddUserExamAnswersAsync(List<UserExamAnswer> answers)
        {
            await _context.UserExamAnswers.AddRangeAsync(answers);
        }
        public async Task<bool> QuestionTypeExistsAsync(string questionTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionTypes
                .AnyAsync(qt => qt.QuestionTypeId == questionTypeId, cancellationToken);
        }

        public async Task<List<QuestionBank>> GetRandomQuestionsByTypeAsync(string questionTypeId, int count, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionBank
                .Include(q => q.QuestionOptions)
                .Include(q => q.Passage)
                .Where(q => q.QuestionTypeId == questionTypeId)
                .OrderBy(q => Guid.NewGuid())
                .Take(count)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<string>> GetValidQuestionTypeIdsAsync(List<string> questionTypeIds, CancellationToken cancellationToken = default)
        {
            return await _context.QuestionTypes
                .Where(qt => questionTypeIds.Contains(qt.QuestionTypeId))
                .Select(qt => qt.QuestionTypeId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<QuestionTypeMenuItem>> GetQuestionTypeMenuAsync(
            List<string> questionTypeIds,
            CancellationToken cancellationToken = default)
        {
            return await _context.QuestionTypes
                .Where(qt => questionTypeIds.Contains(qt.QuestionTypeId) && qt.IsActive)
                .Select(qt => new QuestionTypeMenuItem
                {
                    QuestionTypeId = qt.QuestionTypeId,
                    Code = qt.Code,
                    Name = qt.Name,
                    Description = qt.Description,
                    Skill = qt.Skill.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<GrammarMenuItem>> GetGrammarMenuAsync(
            List<string> questionTypeIds,
            CurrentTopikLevel level,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Grammars
                .Where(g => g.Level == level || g.Level == (CurrentTopikLevel)((int)level + 1));

            if (questionTypeIds.Any())
            {
                query = _context.Grammars
                    .Where(g => questionTypeIds.Contains(g.RelatedQuestionTypeId)
                             || g.Level == level);
            }

            return await query
                .OrderBy(g => g.RelatedQuestionTypeId == null)
                .Take(20)
                .Select(g => new GrammarMenuItem
                {
                    GrammarId = g.GrammarId,
                    Title = g.Title,
                    Syntaxes = g.Syntaxes,
                    Description = g.Description,
                    RelatedQuestionTypeId = g.RelatedQuestionTypeId
                })
                .ToListAsync(cancellationToken);
        }
        public async Task<List<string>> GetValidQuestionTypeIdsByLevelAsync(
            CurrentTopikLevel level,
            CancellationToken cancellationToken = default)
        {
            int levelVal = (int)level;
            var examType = levelVal <= (int)CurrentTopikLevel.Level_2
                ? ExamType.TopikI
                : ExamType.TopikII;

            return await _context.QuestionTypes
                .Where(qt => qt.IsActive && qt.ExamType == examType)
                .OrderBy(qt => qt.OrderIndex)
                .Select(qt => qt.QuestionTypeId)
                .ToListAsync(cancellationToken);
        }
        public async Task<Exam?> GetEntranceExamByConfigKeyAsync(
            string configKey,
            CancellationToken cancellationToken = default)
        {
            var config = await _context.SystemConfig
                .FirstOrDefaultAsync(c => c.Key == configKey && c.IsActive, cancellationToken);

            if (config == null || string.IsNullOrEmpty(config.Value))
                return null;

            return await _context.Exams
                .FirstOrDefaultAsync(e => e.ExamId == config.Value
                                       && e.Status == ExamStatus.Published,
                    cancellationToken);
        }
        public async Task<List<(string QuestionTypeId, int OrderIndex)>> GetExpansionQuestionTypeIdsAsync(
            ExamType examType,
            List<string> excludeQuestionTypeIds,
            int lastCoveredOrderIndex,
            int take,
            CancellationToken cancellationToken = default)
        {
            return await _context.QuestionTypes
                .Where(qt => qt.IsActive
                          && qt.ExamType == examType
                          && !excludeQuestionTypeIds.Contains(qt.QuestionTypeId)
                          && qt.OrderIndex > lastCoveredOrderIndex)
                .OrderBy(qt => qt.OrderIndex)
                .Take(take)
                .Select(qt => new { qt.QuestionTypeId, qt.OrderIndex })
                .ToListAsync(cancellationToken)
                .ContinueWith(t => t.Result
                    .Select(x => (x.QuestionTypeId, x.OrderIndex))
                    .ToList());
        }

        public async Task<List<string>> GetCoreTypesByTargetAimAsync(
            TargetAimLevel targetAim,
            CancellationToken cancellationToken = default)
        {
            var config = await _context.TopikLevelConfigs
                .FirstOrDefaultAsync(
                    c => c.TargetAimLevel == (int)targetAim && c.IsActive,
                    cancellationToken);

            if (config == null)
                return new List<string>();

            var examType = config.ExamGroup == 1 ? ExamType.TopikI : ExamType.TopikII;

            var allTypes = await _context.QuestionTypes
                .Where(qt => qt.IsActive && qt.ExamType == examType)
                .Select(qt => new
                {
                    qt.QuestionTypeId,
                    qt.Code,
                    qt.Skill
                })
                .ToListAsync(cancellationToken);

            int maxCoreWritingQuestion = config.TargetWritingQuestions > 0
                ? 50 + config.TargetWritingQuestions
                : 0;

            var coreTypeIds = new List<string>();

            foreach (var qt in allTypes)
            {
                int startQ = ParseStartQuestion(qt.Code);
                if (startQ < 0) continue;

                bool isCore = qt.Skill switch
                {
                    QuestionSkill.Listening => startQ <= config.TargetListeningQuestions,
                    QuestionSkill.Reading => startQ <= config.TargetReadingQuestions,
                    QuestionSkill.Writing => maxCoreWritingQuestion > 0
                                              && startQ <= maxCoreWritingQuestion,
                    _ => false
                };

                if (isCore)
                    coreTypeIds.Add(qt.QuestionTypeId);
            }

            return coreTypeIds;
        }

        private static int ParseStartQuestion(string code)
        {
            try
            {
                var qIndex = code.IndexOf("_Q", StringComparison.OrdinalIgnoreCase);
                if (qIndex < 0) return -1;

                var afterQ = code[(qIndex + 2)..];
                var underscoreIdx = afterQ.IndexOf('_');
                var startStr = underscoreIdx >= 0
                    ? afterQ[..underscoreIdx]
                    : afterQ;

                return int.TryParse(startStr, out var num) ? num : -1;
            }
            catch
            {
                return -1;
            }
        }
    }
}