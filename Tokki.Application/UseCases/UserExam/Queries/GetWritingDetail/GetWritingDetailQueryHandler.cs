using MediatR;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // Bổ sung namespace này để parse JSON
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetWritingDetail
{
    public class GetWritingDetailQueryHandler : IRequestHandler<GetWritingDetailQuery, OperationResult<WritingDetailResponse>>
    {
        private readonly IUserExamRepository _repository;

        public GetWritingDetailQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<WritingDetailResponse>> Handle(GetWritingDetailQuery request, CancellationToken token)
        {
            var session = await _repository.GetWritingDetailAsync(request.UserExamId, token);

            if (session == null)
                return OperationResult<WritingDetailResponse>.Failure("Không tìm thấy kết quả bài thi.", 404);

            if (session.Status == UserExamStatus.InProgress)
                return OperationResult<WritingDetailResponse>.Failure("Bạn chưa nộp bài thi nên chưa thể xem kết quả.", 400);

            var templateParts = session.Exam?.ExamTemplate?.TemplateParts;
            if (templateParts == null || !templateParts.Any())
                return OperationResult<WritingDetailResponse>.Failure("Cấu trúc đề thi bị lỗi.", 400);

            // FIX 1: Sort các Part thuộc kỹ năng Writing theo thứ tự câu hỏi bắt đầu
            var writingParts = templateParts
                .Where(p => p.Skill == QuestionSkill.Writing)
                .OrderBy(p => p.QuestionFrom)
                .ToList();

            var response = new WritingDetailResponse();
            var groupsDto = new List<QuestionWritingResultGroupDto>();
            QuestionWritingResultGroupDto? currentGroup = null;

            foreach (var part in writingParts)
            {
                int from = part.QuestionFrom;
                int to = part.QuestionTo;
                double mark = part.Mark;

                // FIX 2: Đảm bảo các bài viết tự luận (WritingAnswers) được sort theo OrderIndex
                var partAnswers = session.UserExamWritingAnswers
                    .Where(a => a.OrderIndex >= from && a.OrderIndex <= to)
                    .OrderBy(a => a.OrderIndex)
                    .ToList();

                response.TotalQuestions += (to - from + 1);
                response.MaxScore += (to - from + 1) * mark;

                foreach (var answer in partAnswers)
                {
                    response.Score += answer.Score ?? 0;

                    var question = answer.Question;

                    // Xử lý AI Analysis JSON
                    object? parsedAiAnalysis = null;
                    if (!string.IsNullOrWhiteSpace(answer.AiAnalysisJson))
                    {
                        try
                        {
                            parsedAiAnalysis = JsonSerializer.Deserialize<object>(answer.AiAnalysisJson);
                        }
                        catch
                        {
                            parsedAiAnalysis = new
                            {
                                isParseError = true,
                                rawText = answer.AiAnalysisJson
                            };
                        }
                    }

                    var questionDto = new QuestionWritingResultDto
                    {
                        QuestionNo = answer.OrderIndex,
                        Content = question.Content,
                        AnswerContent = answer.AnswerContent,
                        WordCount = answer.WordCount,
                        Score = answer.Score,
                        AiAnalysis = parsedAiAnalysis,
                        GradedAt = answer.GradedAt
                    };

                    // Logic Grouping (Dựa trên Media/Passage của câu hỏi Writing)
                    bool isSameGroup = currentGroup != null &&
                                       currentGroup.SharedMediaUrl == question.MediaUrl &&
                                       currentGroup.SharedPassageContent == question.Passage?.Content;

                    if (!isSameGroup)
                    {
                        currentGroup = new QuestionWritingResultGroupDto
                        {
                            SharedMediaUrl = question.MediaUrl,
                            SharedMediaType = GetMediaType(question.MediaUrl),
                            SharedPassageContent = question.Passage?.Content,
                            Questions = new List<QuestionWritingResultDto>()
                        };
                        groupsDto.Add(currentGroup);
                    }

                    currentGroup.Questions.Add(questionDto);
                }
            }

            response.QuestionGroups = groupsDto;

            return OperationResult<WritingDetailResponse>.Success(response);
        }
        private string GetMediaType(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "None";
            var ext = Path.GetExtension(url).ToLower();
            if (new[] { ".mp3", ".wav", ".ogg", ".m4a" }.Contains(ext)) return "Audio";
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext)) return "Image";
            return "Unknown";
        }
    }
}