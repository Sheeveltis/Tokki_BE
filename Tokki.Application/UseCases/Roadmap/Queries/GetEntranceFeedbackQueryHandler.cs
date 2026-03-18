using MediatR;
using Tokki.Application.Common.Constants;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetEntranceFeedback
{
    public class GetEntranceFeedbackQueryHandler
        : IRequestHandler<GetEntranceFeedbackQuery, OperationResult<EntranceFeedbackResult>>
    {
        private readonly IUserExamRepository _userExamRepository;
        private readonly IAiRoadmapService _aiRoadmapService;
        private readonly IMediator _mediator;

        public GetEntranceFeedbackQueryHandler(
            IUserExamRepository userExamRepository,
            IAiRoadmapService aiRoadmapService,
            IMediator mediator)
        {
            _userExamRepository = userExamRepository;
            _aiRoadmapService = aiRoadmapService;
            _mediator = mediator;
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

            var calculatedLevel = CalculateLevel(
                request.TargetAim,
                skillData.Listening.Score,
                skillData.Reading.Score,
                skillData.Writing.Score);

            var finalLevel = (CurrentTopikLevel)Math.Min(
                (int)request.SelfDeclaredLevel,
                (int)calculatedLevel);

            await _userExamRepository.SaveSelfDeclaredLevelAsync(
                request.UserExamId,
                request.SelfDeclaredLevel,
                cancellationToken);

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
            var durationOptions = CalculateDurationOptions(totalWeakTypes);
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
                SuggestedCurrentLevel = finalLevel,
                SuggestedCurrentLevelName = GetLevelDisplayName(finalLevel)
            };

            return OperationResult<EntranceFeedbackResult>.Success(result);
        }

        private static CurrentTopikLevel CalculateLevel(
            TargetAimLevel targetAim,
            double listeningScore,
            double readingScore,
            double writingScore)
        {
            double totalScore = listeningScore + readingScore + writingScore;

            if (targetAim == TargetAimLevel.Topik_I_Level1
             || targetAim == TargetAimLevel.Topik_I_Level2)
            {
                double topikIScore = listeningScore + readingScore;
                if (topikIScore >= 140) return CurrentTopikLevel.Level_2;
                if (topikIScore >= 80) return CurrentTopikLevel.Level_1;
                return CurrentTopikLevel.Pre_Topik;
            }

            if (totalScore >= 190) return CurrentTopikLevel.Level_5;
            if (totalScore >= 150) return CurrentTopikLevel.Level_4;
            if (totalScore >= 120) return CurrentTopikLevel.Level_3;
            return CurrentTopikLevel.Pre_Topik_II;
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

        private static List<EntranceDurationOption> CalculateDurationOptions(int totalWeakTypes)
        {
            bool allow30 = totalWeakTypes <= 3;
            bool allow60 = totalWeakTypes <= 8;

            bool recommend30 = totalWeakTypes <= 2;
            bool recommend60 = !recommend30 && totalWeakTypes <= 6;
            bool recommend90 = !recommend30 && !recommend60;

            return new List<EntranceDurationOption>
            {
                new() {
                    Days = 30, Available = allow30, Recommended = recommend30,
                    Reason = allow30
                        ? "Số dạng cần cải thiện ít, 30 ngày là đủ nếu học đều đặn."
                        : $"Bạn có {totalWeakTypes} dạng cần cải thiện, 30 ngày không đủ."
                },
                new() {
                    Days = 60, Available = allow60, Recommended = recommend60,
                    Reason = allow60
                        ? $"Phù hợp để cải thiện {totalWeakTypes} dạng một cách vững chắc."
                        : $"Với {totalWeakTypes} dạng cần học, nên dành ít nhất 90 ngày."
                },
                new() {
                    Days = 90, Available = true, Recommended = recommend90,
                    Reason = recommend90
                        ? $"Lựa chọn tốt nhất cho {totalWeakTypes} dạng cần cải thiện."
                        : "Lộ trình thoải mái nếu bạn muốn học chắc, không áp lực."
                }
            };
        }
    }
}