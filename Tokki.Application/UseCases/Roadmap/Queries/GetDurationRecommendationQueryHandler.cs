using MediatR;
using Tokki.Application.Common.Constants;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetDurationRecommendation
{
    public class GetDurationRecommendationQueryHandler
        : IRequestHandler<GetDurationRecommendationQuery, OperationResult<DurationRecommendationResult>>
    {
        public GetDurationRecommendationQueryHandler() { }

        public Task<OperationResult<DurationRecommendationResult>> Handle(
            GetDurationRecommendationQuery request,
            CancellationToken cancellationToken)
        {
            if (!TopikLevelConfig.Levels.TryGetValue(request.TargetAim, out var levelInfo))
                return Task.FromResult(
                    OperationResult<DurationRecommendationResult>.Failure(
                        "Mục tiêu học tập không hợp lệ.", 400));

            int totalWeakTypes = request.WeakQuestionTypeIds?.Count ?? 0;

            var options = CalculateDurationOptions(totalWeakTypes, levelInfo.DisplayName);

            var result = new DurationRecommendationResult
            {
                TotalWeakTypes = totalWeakTypes,
                Options = options
            };

            return Task.FromResult(
                OperationResult<DurationRecommendationResult>.Success(result));
        }

        private static List<DurationOption> CalculateDurationOptions(
            int totalWeakTypes,
            string displayName)
        {
            bool allow30 = totalWeakTypes <= 3;
            bool allow60 = totalWeakTypes <= 8;
            bool allow90 = true;

            bool recommend30 = totalWeakTypes <= 2;
            bool recommend60 = !recommend30 && totalWeakTypes <= 6;
            bool recommend90 = !recommend30 && !recommend60;

            string reason30 = allow30
                ? "Số dạng cần cải thiện ít, 30 ngày là đủ nếu học đều đặn."
                : $"Bạn có {totalWeakTypes} dạng cần cải thiện, 30 ngày không đủ để ôn chắc.";

            string reason60 = allow60
                ? $"Phù hợp để học {totalWeakTypes} dạng một cách vững chắc."
                : $"Với {totalWeakTypes} dạng cần học, nên dành ít nhất 90 ngày.";

            string reason90 = recommend90
                ? $"Lựa chọn tốt nhất cho {totalWeakTypes} dạng cần cải thiện. Đảm bảo đủ thời gian ôn luyện."
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