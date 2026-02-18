using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetInProgressExam
{
    public class GetInProgressExamQueryHandler : IRequestHandler<GetInProgressExamQuery, OperationResult<UserTakeExamResponse>>
    {
        private readonly IUserExamRepository _repository;

        public GetInProgressExamQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<UserTakeExamResponse>> Handle(GetInProgressExamQuery request, CancellationToken token)
        {
            var session = await _repository.GetByIdAsync(request.UserExamId, token);

            if (session == null)
                return OperationResult<UserTakeExamResponse>.Failure("Không tìm thấy phiên làm bài đang diễn ra", 404);

            return OperationResult<UserTakeExamResponse>.Success(MapToResponse(session, false));
        }

        private UserTakeExamResponse MapToResponse(Domain.Entities.UserExam session, bool isShuffleOptions)
        {
            var elapsedSeconds = (int)(DateTime.UtcNow - session.StartTime).TotalSeconds;
            var duration = session.Exam.Duration * 60;
            var remaining = Math.Max(0, duration - elapsedSeconds);

            var questionsMap = new Dictionary<int, QuestionAnswerMetadata>();

            foreach (var a in session.UserExamAnswers)
                questionsMap[a.OrderIndex] = new QuestionAnswerMetadata
                {
                    AnswerId = a.UserExamAnswerId,
                    Question = a.Question,
                    IsWriting = false,
                    SelectedOptionId = a.SelectedOptionId
                };

            foreach (var w in session.UserExamWritingAnswers)
                questionsMap[w.OrderIndex] = new QuestionAnswerMetadata
                {
                    AnswerId = w.UserExamWritingAnswerId,
                    Question = w.Question,
                    IsWriting = true,
                    AnswerContent = w.AnswerContent
                };

            var response = new UserTakeExamResponse
            {
                UserExamId = session.UserExamId,
                ExamId = session.ExamId,
                Title = session.Exam.Title,
                Duration = session.Exam.Duration,
                TotalQuestions = questionsMap.Count,
                TimeRemaining = remaining,
                Part = new ExamSkillsDto()
            };

            var parts = session.Exam.ExamTemplate.TemplateParts.OrderBy(p => p.QuestionFrom).ToList();

            response.Part.Listening = parts.Where(p => p.Skill == QuestionSkill.Listening)
                                           .Select(p => MapToPartDto(p, questionsMap, isShuffleOptions)).ToList();

            response.Part.Reading = parts.Where(p => p.Skill == QuestionSkill.Reading)
                                         .Select(p => MapToPartDto(p, questionsMap, isShuffleOptions)).ToList();

            response.Part.Writing = parts.Where(p => p.Skill == QuestionSkill.Writing)
                                         .Select(p => MapToPartWritingDto(p, questionsMap)).ToList();

            return response;
        }

        private ExamPartDto MapToPartDto(TemplatePart part, Dictionary<int, QuestionAnswerMetadata> questionsMap, bool shuffleOptions)
        {
            var questionsDto = new List<ExamQuestionDto>();

            for (int i = part.QuestionFrom; i <= part.QuestionTo; i++)
            {
                if (questionsMap.TryGetValue(i, out var item) && !item.IsWriting)
                {
                    var q = item.Question;
                    var options = q.QuestionOptions.Select(o => new ExamOptionDto
                    {
                        OptionId = o.OptionId,
                        KeyOption = o.KeyOption,
                        Content = o.Content,
                        ImageUrl = o.ImageUrl
                    }).ToList();

                    if (shuffleOptions && options.Count > 1)
                        options = options.OrderBy(x => Guid.NewGuid()).ToList();

                    questionsDto.Add(new ExamQuestionDto
                    {
                        UserQuestionId = item.AnswerId,
                        QuestionNo = i,
                        Content = q.Content,
                        MediaUrl = q.MediaUrl,
                        MediaType = GetMediaType(q.MediaUrl),
                        PassageContent = q.Passage?.Content,
                        Options = options,
                        SelectedOptionId = item.SelectedOptionId
                    });
                }
            }

            return new ExamPartDto
            {
                PartId = part.TemplatePartId,
                PartName = part.PartTitle,
                Description = part.Instruction ?? string.Empty,
                ExampleUrl = part.ExampleUrl,
                Questions = questionsDto
            };
        }

        private ExamPartWritingDto MapToPartWritingDto(TemplatePart part, Dictionary<int, QuestionAnswerMetadata> questionsMap)
        {
            var questionsDto = new List<ExamQuestionWritingDto>();

            for (int i = part.QuestionFrom; i <= part.QuestionTo; i++)
            {
                if (questionsMap.TryGetValue(i, out var item) && item.IsWriting)
                {
                    var q = item.Question;
                    questionsDto.Add(new ExamQuestionWritingDto
                    {
                        UserQuestionId = item.AnswerId,
                        QuestionNo = i,
                        Content = q.Content,
                        MediaUrl = q.MediaUrl,
                        MediaType = GetMediaType(q.MediaUrl),
                        PassageContent = q.Passage?.Content,
                        AnswerContent = item.AnswerContent,
                        QuestionTypeCode = q.QuestionType?.Code
                    });
                }
            }

            return new ExamPartWritingDto
            {
                PartId = part.TemplatePartId,
                PartName = part.PartTitle,
                Description = part.Instruction ?? string.Empty,
                ExampleUrl = part.ExampleUrl,
                Questions = questionsDto
            };
        }

        private string GetMediaType(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "None";
            var ext = Path.GetExtension(url).ToLower();
            if (new[] { ".mp3", ".wav", ".ogg", ".m4a" }.Contains(ext)) return "Audio";
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext)) return "Image";
            return "Unknown";
        }

        private class QuestionAnswerMetadata
        {
            public string AnswerId { get; set; } = string.Empty;
            public QuestionBank Question { get; set; } = null!;
            public bool IsWriting { get; set; }
            public string? SelectedOptionId { get; set; }
            public string? AnswerContent { get; set; }
        }
    }
}