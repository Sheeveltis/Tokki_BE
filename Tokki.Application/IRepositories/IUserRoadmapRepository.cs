using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

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
    Task<List<ExamQuestion>> GetExamQuestionsForGradingAsync(string examId, CancellationToken cancellationToken = default);
    Task AddUserExamAsync(UserExam userExam);
    Task AddUserExamAnswersAsync(List<UserExamAnswer> answers);
    Task<bool> QuestionTypeExistsAsync(string questionTypeId, CancellationToken cancellationToken = default);
    Task<List<QuestionBank>> GetRandomQuestionsByTypeAsync(string questionTypeId, int count, CancellationToken cancellationToken = default);
    Task<List<string>> GetValidQuestionTypeIdsAsync(List<string> questionTypeIds, CancellationToken cancellationToken = default);
    Task<List<QuestionTypeMenuItem>> GetQuestionTypeMenuAsync(
        List<string> questionTypeIds,
        CancellationToken cancellationToken = default);

    Task<List<GrammarMenuItem>> GetGrammarMenuAsync(
        List<string> questionTypeIds,
        CurrentTopikLevel level,
        CancellationToken cancellationToken = default);
    Task<List<string>> GetValidQuestionTypeIdsByLevelAsync(
    CurrentTopikLevel level,
    CancellationToken cancellationToken = default);
    Task<Exam?> GetEntranceExamByConfigKeyAsync(
    string configKey,
    CancellationToken cancellationToken = default);
}