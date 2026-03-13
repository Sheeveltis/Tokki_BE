using MediatR;
using Tokki.Application.Common.Constants;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetEntranceExam
{
    public class GetEntranceExamQueryHandler
        : IRequestHandler<GetEntranceExamQuery, OperationResult<EntranceExamResult>>
    {
        private readonly IExamRepository _examRepository;

        public GetEntranceExamQueryHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<EntranceExamResult>> Handle(
            GetEntranceExamQuery request,
            CancellationToken cancellationToken)
        {
            if (!TopikLevelConfig.Levels.TryGetValue(request.TargetAim, out var levelInfo))
                return OperationResult<EntranceExamResult>.Failure(
                    "Mục tiêu học tập không hợp lệ.", 400);
            var exam = await _examRepository
                .GetEntranceExamByTypeAsync(levelInfo.EntranceExamType, cancellationToken);

            if (exam == null)
                return OperationResult<EntranceExamResult>.Failure(
                    $"Chưa có đề test đầu vào cho {levelInfo.DisplayName}. Vui lòng liên hệ admin.", 404);

            return OperationResult<EntranceExamResult>.Success(new EntranceExamResult
            {
                ExamId = exam.ExamId,
                Title = exam.Title,
                Duration = exam.Duration,
                ExamGroup = levelInfo.ExamGroup,
                PassScore = levelInfo.PassScore,
                TotalScore = levelInfo.TotalScore
            });
        }
    }
}