using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetReadingDetail
{
    public class GetReadingDetailQueryHandler : IRequestHandler<GetReadingDetailQuery, OperationResult<ReadingDetailResponse>>
    {
        private readonly IUserExamRepository _repository;

        public GetReadingDetailQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<ReadingDetailResponse>> Handle(GetReadingDetailQuery request, CancellationToken token)
        {
            var session = await _repository.GetReadingDetailAsync(request.UserExamId, token);

            if (session == null)
                return OperationResult<ReadingDetailResponse>.Failure("Không tìm thấy kết quả bài thi.", 404);

            if (session.Status == UserExamStatus.InProgress)
                return OperationResult<ReadingDetailResponse>.Failure("Bạn chưa nộp bài thi nên chưa thể xem kết quả.", 400);

            var templateParts = session.Exam?.ExamTemplate?.TemplateParts;
            if (templateParts == null || !templateParts.Any())
                return OperationResult<ReadingDetailResponse>.Failure("Cấu trúc đề thi bị lỗi.", 400);

            var readingParts = templateParts
                .Where(p => p.Skill == QuestionSkill.Reading)
                .OrderBy(p => p.QuestionFrom)
                .ToList();

            var response = new ReadingDetailResponse();
            var groupsDto = new List<QuestionResultGroupDto>();
            QuestionResultGroupDto? currentGroup = null;

            foreach (var part in readingParts)
            {
                int from = part.QuestionFrom;
                int to = part.QuestionTo;
                double mark = part.Mark;

                var partAnswers = session.UserExamAnswers
                    .Where(a => a.OrderIndex >= from && a.OrderIndex <= to)
                    .OrderBy(a => a.OrderIndex)
                    .ToList();

                response.TotalQuestions += (to - from + 1);
                response.MaxScore += (to - from + 1) * mark;

                foreach (var answer in partAnswers)
                {
                    bool isCorrect = answer.IsCorrect ?? false;

                    if (isCorrect)
                    {
                        response.CorrectAnswers++;
                        response.Score += mark;
                    }

                    var question = answer.Question;
                    var correctOption = question.QuestionOptions.FirstOrDefault(o => o.IsCorrect);

                    var questionDto = new QuestionResultDto
                    {
                        QuestionNo = answer.OrderIndex,
                        Content = question.Content,
                        SelectedOptionId = answer.SelectedOptionId,
                        CorrectOptionId = correctOption?.OptionId,
                        IsCorrect = isCorrect,
                        Options = question.QuestionOptions.Select(o => new ExamOptionDto
                        {
                            OptionId = o.OptionId,
                            KeyOption = o.KeyOption,
                            Content = o.Content,
                            ImageUrl = o.ImageUrl
                        }).ToList()
                    };

                    bool isSameGroup = currentGroup != null &&
                                       currentGroup.SharedMediaUrl == question.MediaUrl &&
                                       currentGroup.SharedPassageContent == question.Passage?.Content;

                    if (!isSameGroup)
                    {
                        currentGroup = new QuestionResultGroupDto
                        {
                            SharedMediaUrl = question.MediaUrl,
                            SharedMediaType = GetMediaType(question.MediaUrl),
                            SharedPassageContent = question.Passage?.Content,
                            Questions = new List<QuestionResultDto>()
                        };
                        groupsDto.Add(currentGroup);
                    }

                    currentGroup.Questions.Add(questionDto);
                }
            }

            response.QuestionGroups = groupsDto;

            return OperationResult<ReadingDetailResponse>.Success(response);
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
