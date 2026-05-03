using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.IServices
{
    public interface IExamAssemblyService
    {
        Task<OperationResult<string>> GenerateWeeklyExamAsync(
            string templateId,
            string userId,
            int weekIndex,
            List<string> weakQuestionTypeIds,
            DifficultyLevel targetLevel,
            CancellationToken cancellationToken);
        Task<OperationResult<string>> GenerateWeeklyExamFromScopeAsync(
            string userId,
            int weekIndex,
            List<string> weeklyQuestionTypeIds,
            ExamType examType,
            CancellationToken cancellationToken = default);
        Task<(bool IsValid, List<string> InsufficientTypes)> ValidateQuestionAvailabilityAsync(
            List<string> questionTypeIds,
            CancellationToken cancellationToken = default);

        Task<OperationResult<string>> GenerateTopikStyleExamAsync(
            string userId,
            int weekIndex,
            List<string> weaknessTypeIds,
            ExamType examType,
            CancellationToken cancellationToken = default);
    }
}