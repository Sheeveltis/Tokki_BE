using MediatR;
using Tokki.Application.Common.Constants;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult;
using Tokki.Domain.Constants;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetEntranceFeedback
{
    public class GetEntranceFeedbackQueryHandler
        : IRequestHandler<GetEntranceFeedbackQuery, OperationResult<EntranceFeedbackResult>>
    {
        private readonly IUserExamRepository _userExamRepository;
        private readonly IAiRoadmapService _aiRoadmapService;
        private readonly IMediator _mediator;
        private readonly ITopikLevelConfigRepository _topikLevelConfigRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;

        public GetEntranceFeedbackQueryHandler(
            IUserExamRepository userExamRepository,
            IAiRoadmapService aiRoadmapService,
            IMediator mediator,
            ITopikLevelConfigRepository topikLevelConfigRepository,
            ISystemConfigRepository systemConfigRepository)
        {
            _userExamRepository = userExamRepository;
            _aiRoadmapService = aiRoadmapService;
            _mediator = mediator;
            _topikLevelConfigRepository = topikLevelConfigRepository;
            _systemConfigRepository = systemConfigRepository;
        }

        public async Task<OperationResult<EntranceFeedbackResult>> Handle(
            GetEntranceFeedbackQuery request,
            CancellationToken cancellationToken)
        {
            var isPending = await _userExamRepository
                .HasPendingWritingAnswersAsync(request.UserExamId, cancellationToken);

            if (isPending)
                return OperationResult<EntranceFeedbackResult>.Failure(
                    "Bài viết đang được chấm, vui lòng thử lại sau ít phút.", 202);

            var examResult = await _mediator.Send(
                new GetUserExamResultQuery { UserExamId = request.UserExamId },
                cancellationToken);

            if (!examResult.IsSuccess || examResult.Data == null)
                return OperationResult<EntranceFeedbackResult>.Failure(
                    "Không tìm thấy kết quả bài thi.", 404);

            var skillData = examResult.Data;

            var levelConfigs = await _topikLevelConfigRepository.GetAllAsync();
            var calculatedLevel = CalculateLevel(
                request.TargetAim,
                skillData.Listening.Score,
                skillData.Reading.Score,
                skillData.Writing.Score,
                levelConfigs);

            var finalLevel = calculatedLevel;

            var questionTypes = await _userExamRepository
                .GetIncorrectQuestionTypesByExamIdAsync(
                    request.UserExamId, cancellationToken);

            if (questionTypes == null)
                return OperationResult<EntranceFeedbackResult>.Failure(
                    "Không tìm thấy kết quả phân tích.", 404);

            var readingIssues = questionTypes
                .Where(qt => qt.Skill == QuestionSkill.Reading)
                .Select(qt => new WeakTypeDto
                {
                    QuestionTypeId = qt.QuestionTypeId,
                    Code = qt.Code,
                    Name = qt.Name
                }).ToList();

            var listeningIssues = questionTypes
                .Where(qt => qt.Skill == QuestionSkill.Listening)
                .Select(qt => new WeakTypeDto
                {
                    QuestionTypeId = qt.QuestionTypeId,
                    Code = qt.Code,
                    Name = qt.Name
                }).ToList();

            var writingIssues = questionTypes
                .Where(qt => qt.Skill == QuestionSkill.Writing)
                .Select(qt => new WeakTypeDto
                {
                    QuestionTypeId = qt.QuestionTypeId,
                    Code = qt.Code,
                    Name = qt.Name
                }).ToList();

            int totalWeakTypes = questionTypes.Count;
            double avgDifficulty = questionTypes.Any()
                ? questionTypes.Average(qt => (int)qt.Difficulty)
                : 1.0;
            var durationOptions = await CalculateDurationOptionsAsync(totalWeakTypes, avgDifficulty);
            int recommendedDays = durationOptions
                .FirstOrDefault(o => o.Recommended)?.Days ?? 90;

            var aiFeedback = await _aiRoadmapService.GenerateEntranceFeedbackAsync(
                request.TargetAim,
                readingIssues.Count,
                listeningIssues.Count,
                writingIssues.Count,
                readingIssues.Select(r => r.Name).ToList(),
                listeningIssues.Select(l => l.Name).ToList(),
                writingIssues.Select(w => w.Name).ToList(),
                recommendedDays
            );

            if (string.IsNullOrEmpty(aiFeedback))
            {
                aiFeedback = $"Dựa trên kết quả bài test, bạn có {totalWeakTypes} dạng câu cần cải thiện " +
                             $"({readingIssues.Count} Đọc, {listeningIssues.Count} Nghe, {writingIssues.Count} Viết). " +
                             $"Chúng tôi đề xuất lộ trình {recommendedDays} ngày.";
            }

            var result = new EntranceFeedbackResult
            {
                AiFeedback = aiFeedback,
                TotalWeakTypes = totalWeakTypes,
                ReadingWeakCount = readingIssues.Count,
                ListeningWeakCount = listeningIssues.Count,
                WritingWeakCount = writingIssues.Count,
                ReadingIssues = readingIssues,
                ListeningIssues = listeningIssues,
                WritingIssues = writingIssues,
                DurationOptions = durationOptions,
                SuggestedCurrentLevel = calculatedLevel,
                SuggestedCurrentLevelName = GetLevelDisplayName(finalLevel)
            };

            return OperationResult<EntranceFeedbackResult>.Success(result);
        }

        private static CurrentTopikLevel CalculateLevel(
            TargetAimLevel targetAim,
            double listeningScore,
            double readingScore,
            double writingScore,
            List<Tokki.Domain.Entities.TopikLevelConfig> levelConfigs)
        {
            bool isTopikI = targetAim == TargetAimLevel.Topik_I_Level1
                         || targetAim == TargetAimLevel.Topik_I_Level2;

            double score = isTopikI
                ? listeningScore + readingScore
                : listeningScore + readingScore + writingScore;

            int examGroup = isTopikI ? 1 : 2;

            foreach (var config in levelConfigs
                .Where(c => c.ExamGroup == examGroup && c.IsActive)
                .OrderByDescending(c => c.PassScore))
            {
                if (score >= config.PassScore)
                    return (CurrentTopikLevel)config.TargetAimLevel;
            }

            return isTopikI
                ? CurrentTopikLevel.Pre_Topik
                : CurrentTopikLevel.Pre_Topik_II;
        }

        private static string GetLevelDisplayName(CurrentTopikLevel level) => level switch
        {
            CurrentTopikLevel.Pre_Topik => "Chưa có trình độ TOPIK",
            CurrentTopikLevel.Level_1 => "TOPIK I - Level 1",
            CurrentTopikLevel.Level_2 => "TOPIK I - Level 2",
            CurrentTopikLevel.Pre_Topik_II => "Đang xây dựng nền tảng TOPIK II",
            CurrentTopikLevel.Level_3 => "TOPIK II - Level 3",
            CurrentTopikLevel.Level_4 => "TOPIK II - Level 4",
            CurrentTopikLevel.Level_5 => "TOPIK II - Level 5",
            CurrentTopikLevel.Level_6 => "TOPIK II - Level 6",
            _ => "Không xác định"
        };

        private async Task<int> GetIntConfigAsync(string key, int fallback)
        {
            try
            {
                var cfg = await _systemConfigRepository.GetByKeyAsync(key);
                if (cfg is { IsActive: true, Value: not null }
                    && int.TryParse(cfg.Value, out int parsed))
                    return parsed;
            }
            catch {  }
            return fallback;
        }

        private async Task<List<EntranceDurationOption>> CalculateDurationOptionsAsync(
            int totalWeakTypes, double avgDifficulty)
        {
            int minPerWeek      = await GetIntConfigAsync(PromptConfigKeys.RoadmapMinTypesPerWeek,  3);
            int maxPerWeek      = await GetIntConfigAsync(PromptConfigKeys.RoadmapMaxTypesPerWeek,  5);
            int maxDifficulty   = await GetIntConfigAsync(PromptConfigKeys.RoadmapMaxDifficulty,    4);
            int retryRatePct    = await GetIntConfigAsync(PromptConfigKeys.RoadmapRetryRatePercent, 50);

            int N = totalWeakTypes;

            double rawCapacity = maxDifficulty > 1
                ? maxPerWeek - (avgDifficulty - 1.0) * (maxPerWeek - minPerWeek) / (maxDifficulty - 1.0)
                : maxPerWeek;
            int effectiveCapacity = Math.Clamp((int)Math.Round(rawCapacity), minPerWeek, maxPerWeek);

            int coreWeeks = (int)Math.Ceiling((double)N / effectiveCapacity);

            int expectedRetries = (int)Math.Ceiling(N * retryRatePct / 100.0);
            int retryBuffer     = (int)Math.Ceiling((double)expectedRetries / effectiveCapacity);

            int totalWeeks      = coreWeeks + retryBuffer;
            int recommendedDays = totalWeeks * 7;

            bool allow30 = recommendedDays <= 30;
            bool allow60 = recommendedDays <= 60;

            bool recommend30 = allow30;
            bool recommend60 = !recommend30 && allow60;
            bool recommend90 = !recommend30 && !recommend60;

            return new List<EntranceDurationOption>
            {
                new() {
                    Days = 30, Available = allow30, Recommended = recommend30,
                    Reason = allow30
                        ? "Số dạng cần cải thiện ít, 30 ngày là đủ nếu học đều đặn."
                        : $"Bạn có {N} dạng cần cải thiện, 30 ngày không đủ để đảm bảo chất lượng."
                },
                new() {
                    Days = 60, Available = allow60, Recommended = recommend60,
                    Reason = allow60
                        ? $"Phù hợp để cải thiện {N} dạng một cách vững chắc, có thời gian ôn lại các dạng khó."
                        : $"Với {N} dạng cần học, nên dành ít nhất 90 ngày để đảm bảo có thể ôn lại các dạng chưa pass."
                },
                new() {
                    Days = 90, Available = true, Recommended = recommend90,
                    Reason = recommend90
                        ? $"Lựa chọn tốt nhất cho {N} dạng cần cải thiện, đảm bảo đủ thời gian học và ôn lại các dạng khó."
                        : "Lộ trình thoải mái nếu bạn muốn học chắc, không áp lực."
                }
            };
        }
    }
}