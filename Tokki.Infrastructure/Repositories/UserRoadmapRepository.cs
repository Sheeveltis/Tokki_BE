using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
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
            return await _context.UserRoadmaps
                .Include(r => r.Weeks)
                    .ThenInclude(w => w.DailyTasks)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CurrentStatus == UserRoadmapStatus.Active, cancellationToken);
        }
        public async Task<RoadmapDailyTask?> GetTaskByIdAsync(string taskId, CancellationToken cancellationToken = default)
        {
            return await _context.RoadmapDailyTasks
                .Include(t => t.RoadmapWeek)
                .ThenInclude(w => w.UserRoadmap)
                .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);
        }
        public async Task<RoadmapWeek?> GetWeekByIdAsync(string weekId, CancellationToken cancellationToken)
        {
            return await _context.RoadmapWeeks
                .Include(w => w.UserRoadmap)
                .Include(w => w.WeeklyExam)
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
    }
}