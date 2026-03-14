using MediatR;
using Tokki.Application.Common.Constants;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetDurationRecommendation
{
    public class GetDurationRecommendationQueryHandler
        : IRequestHandler<GetDurationRecommendationQuery, OperationResult<DurationRecommendationResult>>
    {
        private readonly IUserRoadmapRepository _repository;

        private static readonly Dictionary<TargetAimLevel, (int PassScore, int TotalScore)> TopikThresholds = new()
        {
            { TargetAimLevel.Topik_I_Level1,  (80,  200) },
            { TargetAimLevel.Topik_I_Level2,  (140, 200) },
            { TargetAimLevel.Topik_II_Level3, (120, 300) },
            { TargetAimLevel.Topik_II_Level4, (150, 300) },
            { TargetAimLevel.Topik_II_Level5, (190, 300) },
            { TargetAimLevel.Topik_II_Level6, (230, 300) },
        };

        public GetDurationRecommendationQueryHandler(IUserRoadmapRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<DurationRecommendationResult>> Handle(
            GetDurationRecommendationQuery request,
            CancellationToken cancellationToken)
        {
            var userExam = await _repository.GetUserExamByExamIdAsync(
                request.ExamId, request.UserId, cancellationToken);

            if (userExam == null)
                return OperationResult<DurationRecommendationResult>.Failure(
                    "Không tìm thấy kết quả bài test đầu vào. Vui lòng hoàn thành bài test trước.", 404);

            if (!TopikLevelConfig.Levels.TryGetValue(request.TargetAim, out var levelInfo))
                return OperationResult<DurationRecommendationResult>.Failure(
                    "Mục tiêu học tập không hợp lệ.", 400);

            int entranceScorePercent = levelInfo.TotalScore > 0
                ? (int)Math.Round((double)userExam.Score / levelInfo.TotalScore * 100)
                : 0;

            int totalWeakTypes = request.WeakQuestionTypeIds?.Count ?? 0;

            var options = CalculateDurationOptions(
                request.TargetAim,
                userExam.Score,
                totalWeakTypes,
                levelInfo.PassScore,
                levelInfo.TotalScore);

            var result = new DurationRecommendationResult
            {
                EntranceScorePercent = entranceScorePercent,
                TotalWeakTypes = totalWeakTypes,
                Options = options
            };

            return OperationResult<DurationRecommendationResult>.Success(result);
        }

        private static List<DurationOption> CalculateDurationOptions(
            TargetAimLevel targetAim,
            int entranceScore,
            int totalWeakTypes,
            int passScore,
            int totalScore)
        {
            // scoreGapRatio: khoảng cách từ điểm hiện tại đến điểm đỗ,
            // tính theo tỉ lệ so với điểm đỗ.
            // Ví dụ: aim level 4 cần 150đ, user đạt 60đ
            // → gap = (150 - 60) / 150 = 0.6 → cần học nhiều
            double scoreGapRatio = passScore > 0
                ? (double)(passScore - entranceScore) / passScore
                : 0;

            scoreGapRatio = Math.Max(0, scoreGapRatio);

            bool alreadyPassed = entranceScore >= passScore;

            bool allow30 = scoreGapRatio <= 0.35 && totalWeakTypes <= 4;
            bool allow60 = scoreGapRatio <= 0.65;
            bool allow90 = true;

            bool recommend30 = allow30 && scoreGapRatio <= 0.20 && totalWeakTypes <= 2;
            bool recommend60 = !recommend30 && allow60;
            bool recommend90 = !recommend30 && !recommend60;

            string reason30;
            if (alreadyPassed)
                reason30 = "Bạn đã đạt ngưỡng điểm mục tiêu! Lộ trình 30 ngày đủ để củng cố.";
            else if (allow30)
                reason30 = "Phù hợp nếu bạn đã có nền tảng tốt và học đều đặn mỗi ngày.";
            else
                reason30 = $"Khoảng cách đến mục tiêu còn lớn ({(int)(scoreGapRatio * 100)}% điểm gap), 30 ngày chưa đủ.";

            string reason60;
            if (allow60)
                reason60 = totalWeakTypes > 4
                    ? $"Bạn có {totalWeakTypes} dạng cần cải thiện, 60 ngày là lựa chọn cân bằng."
                    : "Khả thi nếu học đều đặn, có thời gian ôn tập từng dạng.";
            else
                reason60 = $"Khoảng cách điểm quá lớn ({(int)(scoreGapRatio * 100)}%), nên dành ít nhất 90 ngày.";

            string reason90 = recommend90
                ? $"Phù hợp nhất với trình độ hiện tại ({totalWeakTypes} dạng cần học). Đảm bảo có đủ thời gian."
                : "Lộ trình thoải mái nếu bạn muốn học chắc, không áp lực.";

            return new List<DurationOption>
            {
                new() { Days = 30, Available = allow30, Recommended = recommend30, Reason = reason30 },
                new() { Days = 60, Available = allow60, Recommended = recommend60, Reason = reason60 },
                new() { Days = 90, Available = allow90, Recommended = recommend90, Reason = reason90 }
            };
        }
    }
}