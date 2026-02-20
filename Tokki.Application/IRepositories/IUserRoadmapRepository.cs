using Tokki.Domain.Entities;

public interface IUserRoadmapRepository
{
    Task AddAsync(UserRoadmap roadmap);
    Task <bool>SaveChangesAsync(CancellationToken cancellationToken);
    Task<UserRoadmap?> GetActiveRoadmapByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<RoadmapDailyTask?> GetTaskByIdAsync(string taskId, CancellationToken cancellationToken = default);
    Task<RoadmapWeek?> GetWeekByIdAsync(string weekId, CancellationToken cancellationToken = default);
    Task<RoadmapWeek?> GetWeekByIndexAsync(string roadmapId, int weekIndex, CancellationToken cancellationToken = default);
    Task<int> GetExamScoreAsync(string examId, string userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetWeakQuestionTypesFromExamAsync(string userExamId, CancellationToken cancellationToken = default);
    Task<UserExam?> GetUserExamByExamIdAsync(string examId, string userId, CancellationToken cancellationToken = default);
}