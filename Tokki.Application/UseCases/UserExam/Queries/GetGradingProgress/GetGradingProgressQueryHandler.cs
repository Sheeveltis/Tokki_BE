using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetGradingProgress
{
    public class GetGradingProgressQueryHandler : IRequestHandler<GetGradingProgressQuery, OperationResult<GradingProgressResponse>>
    {
        private readonly IUserExamRepository _repository;

        public GetGradingProgressQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<GradingProgressResponse>> Handle(GetGradingProgressQuery request, CancellationToken token)
        {
            var session = await _repository.GetByIdWithWritingDetailsAsync(request.UserExamId, token);

            if (session == null)
            {
                return OperationResult<GradingProgressResponse>.Failure("Không tìm thấy phiên làm bài", 404);
            }

            if (session.Status != UserExamStatus.Completed)
            {
                return OperationResult<GradingProgressResponse>.Success(new GradingProgressResponse
                {
                    UserExamId = session.UserExamId,
                    ProgressPercentage = 0,
                    IsCompleted = false,
                    Message = "Bài thi đang trong quá trình thực hiện hoặc chưa được nộp."
                });
            }

            var writingParts = session.Exam.ExamTemplate.TemplateParts
                .Where(p => p.Skill == QuestionSkill.Writing)
                .ToList();

            int totalWritingTasks = writingParts.Count;
            
            if (totalWritingTasks == 0)
            {
                return OperationResult<GradingProgressResponse>.Success(new GradingProgressResponse
                {
                    UserExamId = session.UserExamId,
                    ProgressPercentage = 100,
                    IsCompleted = true,
                    Message = "Hoàn tất chấm điểm (Không có phần tự luận)."
                });
            }

            int gradedWritingCount = session.UserExamWritingAnswers
                .Count(hwa => hwa.GradedAt != null);

            int progress = 20 + (int)((double)gradedWritingCount / totalWritingTasks * 80);

            if (progress > 100) progress = 100;

            bool allGraded = gradedWritingCount >= totalWritingTasks;

            return OperationResult<GradingProgressResponse>.Success(new GradingProgressResponse
            {
                UserExamId = session.UserExamId,
                ProgressPercentage = allGraded ? 100 : progress,
                IsCompleted = allGraded,
                Message = allGraded ? "Đã hoàn thành chấm điểm toàn bộ bài thi." : $"Đang tiến hành chấm điểm AI ({gradedWritingCount}/{totalWritingTasks} bài viết)."
            });
        }
    }
}
