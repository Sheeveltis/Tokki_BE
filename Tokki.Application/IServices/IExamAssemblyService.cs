using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.IServices
{
    public interface IExamAssemblyService
    {
        /// <summary>
        /// Tạo bài kiểm tra tuần dựa trên ExamTemplate chuẩn
        /// </summary>
        /// <param name="templateId">ID của template cấu trúc (ví dụ: TOPIK I)</param>
        /// <param name="userId">Người làm bài (để đặt tên đề thi unique)</param>
        /// <param name="weekIndex">Tuần thứ mấy</param>
        /// <param name="weakQuestionTypeIds">Danh sách các dạng bài User đang yếu (để ưu tiên chọn)</param>
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
            CancellationToken cancellationToken = default);
    }
}