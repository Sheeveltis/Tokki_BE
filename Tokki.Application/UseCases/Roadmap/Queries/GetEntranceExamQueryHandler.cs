using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.Constants;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetEntranceExam
{
    public class GetEntranceExamQueryHandler
        : IRequestHandler<GetEntranceExamQuery, OperationResult<EntranceExamResult>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IUserRoadmapRepository _userRoadmapRepository;
        private readonly ITopikLevelConfigService _topikConfig;

        public GetEntranceExamQueryHandler(
            IExamRepository examRepository,
            IUserRoadmapRepository userRoadmapRepository,
            ITopikLevelConfigService topikConfig)
        {
            _examRepository = examRepository;
            _userRoadmapRepository = userRoadmapRepository;
            _topikConfig = topikConfig;
        }

        public async Task<OperationResult<EntranceExamResult>> Handle(
            GetEntranceExamQuery request,
            CancellationToken cancellationToken)
        {
            var levelInfo = await _topikConfig.GetByLevelAsync(request.TargetAim, cancellationToken);

            if (levelInfo == null)
                return OperationResult<EntranceExamResult>.Failure(
                    "Mục tiêu học tập không hợp lệ hoặc chưa được cấu hình.", 400);

            var exam = await _userRoadmapRepository
                .GetEntranceExamByConfigKeyAsync(levelInfo.ConfigKey, cancellationToken);

            if (exam == null)
                return OperationResult<EntranceExamResult>.Failure(
                    $"Chưa có đề test đầu vào cho {levelInfo.DisplayName}. " +
                    $"Vui lòng liên hệ admin để cấu hình đề thi.", 404);

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