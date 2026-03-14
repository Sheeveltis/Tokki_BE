using MediatR;
using Tokki.Application.Common.Constants;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetEntranceFeedback
{
    public class GetEntranceFeedbackQueryHandler
        : IRequestHandler<GetEntranceFeedbackQuery, OperationResult<EntranceFeedbackResult>>
    {
        private readonly IUserExamRepository _userExamRepository;
        private readonly IAiRoadmapService _aiRoadmapService;

        public GetEntranceFeedbackQueryHandler(
            IUserExamRepository userExamRepository,
            IAiRoadmapService aiRoadmapService)
        {
            _userExamRepository = userExamRepository;
            _aiRoadmapService = aiRoadmapService;
        }

        public async Task<OperationResult<EntranceFeedbackResult>> Handle(
            GetEntranceFeedbackQuery request,
            CancellationToken cancellationToken)
        {
            var questionTypes = await _userExamRepository
                .GetIncorrectQuestionTypesByExamIdAsync(
                    request.UserExamId, cancellationToken);

            if (questionTypes == null)
                return OperationResult<EntranceFeedbackResult>.Failure(
                    "Không tìm thấy kết quả bài thi.", 404);

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
                             $"Chúng tôi đề xuất lộ trình {recommendedDays} ngày để đảm bảo bạn có đủ thời gian ôn luyện.";
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
                DurationOptions = durationOptions
            };

            return OperationResult<EntranceFeedbackResult>.Success(result);
        }

        private static List<EntranceDurationOption> CalculateDurationOptions(int totalWeakTypes)
        {
            bool allow30 = totalWeakTypes <= 3;
            bool allow60 = totalWeakTypes <= 8;

            bool recommend30 = totalWeakTypes <= 2;
            bool recommend60 = !recommend30 && totalWeakTypes <= 6;
            bool recommend90 = !recommend30 && !recommend60;

            return new List<EntranceDurationOption>
            {
                new()
                {
                    Days = 30,
                    Available = allow30,
                    Recommended = recommend30,
                    Reason = allow30
                        ? "Số dạng cần cải thiện ít, 30 ngày là đủ nếu học đều đặn."
                        : $"Bạn có {totalWeakTypes} dạng cần cải thiện, 30 ngày không đủ."
                },
                new()
                {
                    Days = 60,
                    Available = allow60,
                    Recommended = recommend60,
                    Reason = allow60
                        ? $"Phù hợp để cải thiện {totalWeakTypes} dạng một cách vững chắc."
                        : $"Với {totalWeakTypes} dạng cần học, nên dành ít nhất 90 ngày."
                },
                new()
                {
                    Days = 90,
                    Available = true,
                    Recommended = recommend90,
                    Reason = recommend90
                        ? $"Lựa chọn tốt nhất cho {totalWeakTypes} dạng cần cải thiện."
                        : "Lộ trình thoải mái nếu bạn muốn học chắc, không áp lực."
                }
            };
        }
    }
}