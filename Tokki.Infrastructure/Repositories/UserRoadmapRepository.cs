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
                .Where(ue => ue.ExamId == examId && ue.UserId == userId && ue.Status == 1)
                .OrderByDescending(ue => ue.SubmitTime) 
                .Select(ue => ue.Score)
                .FirstOrDefaultAsync(cancellationToken);

            return score; 
        }
    }
}